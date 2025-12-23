using System.Collections.Concurrent;
using System.Threading.Channels;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Frame.Types;
using Phantom.Utils.Rpc.Runtime;
using Serilog;

namespace Phantom.Utils.Rpc.Message;

public sealed class MessageSender<TMessageBase> {
	private readonly ILogger logger;
	private readonly IRpcFrameSenderProvider<TMessageBase> frameSenderProvider;
	private readonly MessageReplyTracker messageReplyTracker;
	private readonly UnacknowledgedMessages unacknowledgedMessages = new ();
	
	private readonly Channel<PreparedMessage> messageQueue;
	private readonly Task messageQueueTask;
	
	private uint nextMessageId;
	
	private readonly CancellationTokenSource shutdownCancellationTokenSource = new ();
	
	internal MessageSender(string loggerName, RpcCommonConnectionParameters connectionParameters, IRpcFrameSenderProvider<TMessageBase> frameSenderProvider) {
		this.logger = PhantomLogger.Create<MessageSender<TMessageBase>>(loggerName);
		this.frameSenderProvider = frameSenderProvider;
		this.messageReplyTracker = new MessageReplyTracker(loggerName);
		
		this.messageQueue = Channel.CreateBounded<PreparedMessage>(new BoundedChannelOptions(connectionParameters.MessageQueueCapacity) {
			AllowSynchronousContinuations = false,
			FullMode = BoundedChannelFullMode.Wait,
			SingleReader = true,
			SingleWriter = false,
		});
		
		this.messageQueueTask = ProcessQueue();
	}
	
	public bool TrySend<TMessage>(TMessage message) where TMessage : TMessageBase {
		return messageQueue.Writer.TryWrite(PrepareMessage(message));
	}
	
	public async ValueTask Send<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : TMessageBase {
		await messageQueue.Writer.WriteAsync(PrepareMessage(message), cancellationToken);
	}
	
	public async Task<TReply> Send<TMessage, TReply>(TMessage message, TimeSpan waitForReplyTime, CancellationToken cancellationToken) where TMessage : TMessageBase, ICanReply<TReply> {
		var preparedMessage = PrepareMessage(message);
		var messageId = preparedMessage.MessageId;
		
		messageReplyTracker.RegisterReply<TMessage>(messageId);
		try {
			await messageQueue.Writer.WriteAsync(preparedMessage, cancellationToken);
		} catch (Exception) {
			messageReplyTracker.ForgetReply(messageId);
			throw;
		}
		
		return await messageReplyTracker.WaitForReply<TReply>(messageId, waitForReplyTime, cancellationToken);
	}
	
	private PreparedMessage PrepareMessage<TMessage>(TMessage message) where TMessage : TMessageBase {
		return new PreparedMessage(Interlocked.Increment(ref nextMessageId), typeof(TMessage), (frameSender, messageId, cancellationToken) => frameSender.SendMessage(messageId, message, cancellationToken));
	}
	
	private delegate ValueTask SendMessage(RpcFrameSender<TMessageBase> frameSender, uint messageId, CancellationToken cancellationToken);
	
	private readonly record struct PreparedMessage(uint MessageId, Type MessageType, SendMessage SendFunction) {
		public ValueTask Send(RpcFrameSender<TMessageBase> frameSender, CancellationToken cancellationToken) {
			return SendFunction(frameSender, MessageId, cancellationToken);
		}
	}
	
	private async Task ProcessQueue() {
		CancellationToken cancellationToken = shutdownCancellationTokenSource.Token;
		RpcFrameSender<TMessageBase>? frameSender = null;
		
		while (true) {
			var dequeueMessageTask = messageQueue.Reader.WaitToReadAsync(cancellationToken).AsTask();
			var newFrameSenderTask = frameSenderProvider.NewValueReady(cancellationToken);
			
			var finishedTask = await Task.WhenAny(dequeueMessageTask, newFrameSenderTask);
			if (finishedTask == dequeueMessageTask && !dequeueMessageTask.Result) {
				// Queue closed.
				break;
			}
			
			if (frameSender == null || finishedTask == newFrameSenderTask) {
				frameSender = await frameSenderProvider.GetNewValue(cancellationToken);
				
				foreach (var message in unacknowledgedMessages.GetUnacknowledged()) {
					logger.Warning("Resending message {MessageId} of type {MessageType}.", message.MessageId, message.MessageType.Name);
					await message.Send(frameSender, cancellationToken);
				}
			}
			
			while (messageQueue.Reader.TryRead(out var message)) {
				unacknowledgedMessages.Register(message);
				
				try {
					await message.Send(frameSender, cancellationToken);
				} catch (ChannelClosedException) {
					frameSender = null;
					break;
				}
			}
		}
	}
	
	private sealed class UnacknowledgedMessages {
		private readonly ConcurrentBag<uint> acknowledgedMessageIds = [];
		private readonly SortedDictionary<uint, PreparedMessage> unacknowledgedMessages = new ();
		
		public void Acknowledge(uint messageId) {
			acknowledgedMessageIds.Add(messageId);
		}
		
		public void Register(PreparedMessage message) {
			Update();
			unacknowledgedMessages.Add(message.MessageId, message);
		}
		
		public SortedDictionary<uint, PreparedMessage>.ValueCollection GetUnacknowledged() {
			Update();
			return unacknowledgedMessages.Values;
		}
		
		private void Update() {
			while (acknowledgedMessageIds.TryTake(out uint messageId)) {
				unacknowledgedMessages.Remove(messageId);
			}
		}
	}
	
	internal void ReceiveReply(MessageReplyFrame frame) {
		unacknowledgedMessages.Acknowledge(frame.ReplyingToMessageId);
		messageReplyTracker.ReceiveReply(frame.ReplyingToMessageId, frame.SerializedReply);
	}
	
	internal void ReceiveError(MessageErrorFrame frame) {
		unacknowledgedMessages.Acknowledge(frame.ReplyingToMessageId);
		messageReplyTracker.FailReply(frame.ReplyingToMessageId, MessageErrorException.From(frame.Error));
	}
	
	internal async Task Close(TimeSpan timeout) {
		messageQueue.Writer.TryComplete();
		
		try {
			await messageQueueTask.WaitAsync(timeout);
		} catch (TimeoutException) {
			if (timeout != TimeSpan.Zero) {
				logger.Warning("Could not finish processing message queue before timeout, forcibly shutting it down.");
			}
			
			await shutdownCancellationTokenSource.CancelAsync();
			await messageQueueTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
		} catch (Exception) {
			// Ignore.
		}
		
		messageQueueTask.Dispose();
		shutdownCancellationTokenSource.Dispose();
	}
}
