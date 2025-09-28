using System.Threading.Channels;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Frame;
using Phantom.Utils.Rpc.Frame.Types;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Tasks;
using Serilog;
using Serilog.Events;

namespace Phantom.Utils.Rpc.Runtime;

sealed class RpcFrameSender<TMessageBase> : IMessageReplySender {
	private readonly ILogger logger;
	private readonly IRpcConnectionProvider connectionProvider;
	private readonly MessageTypeMapping<TMessageBase> messageTypeMapping;
	private readonly MessageReceiveTracker messageReceiveTracker = new ();
	
	private readonly Channel<IFrame> frameQueue;
	private readonly Task frameQueueTask;
	private bool isFrameQueueWritable = true;
	
	private readonly Task pingTask;
	private TaskCompletionSource<DateTimeOffset>? pongTask;
	
	private readonly CancellationTokenSource frameQueueCancellationTokenSource = new ();
	private readonly CancellationTokenSource pingCancellationTokenSource = new ();
	
	internal TimeSpan PingInterval { get; }
	
	internal RpcFrameSender(string loggerName, RpcCommonConnectionParameters connectionParameters, IRpcConnectionProvider connectionProvider, MessageTypeMapping<TMessageBase> messageTypeMapping, TimeSpan pingInterval) {
		this.logger = PhantomLogger.Create<RpcFrameSender<TMessageBase>>(loggerName);
		this.connectionProvider = connectionProvider;
		this.messageTypeMapping = messageTypeMapping;
		
		this.frameQueue = Channel.CreateBounded<IFrame>(new BoundedChannelOptions(connectionParameters.FrameQueueCapacity) {
			AllowSynchronousContinuations = false,
			FullMode = BoundedChannelFullMode.Wait,
			SingleReader = true,
			SingleWriter = false,
		});
		
		this.frameQueueTask = ProcessQueue();
		
		this.PingInterval = pingInterval;
		this.pingTask = PingSchedule();
	}
	
	public async ValueTask SendPong(DateTimeOffset pingTime, CancellationToken cancellationToken) {
		await SendFrame(new PongFrame(pingTime), cancellationToken);
	}
	
	public async ValueTask SendMessage<TMessage>(uint messageId, TMessage message, CancellationToken cancellationToken) where TMessage : TMessageBase {
		var frame = messageTypeMapping.CreateFrame(messageId, message);
		logger.Debug("Sending message {MesageId} of type {MessageType} ({MessageBytes} B).", messageId, typeof(TMessage).Name, frame.SerializedMessage.Length);
		await SendFrame(frame, cancellationToken);
	}
	
	async ValueTask IMessageReplySender.SendEmptyReply(uint replyingToMessageId, CancellationToken cancellationToken) {
		logger.Debug("Sending empty reply to message {MessageId}.", replyingToMessageId);
		await SendFrame(new MessageReplyFrame(replyingToMessageId, ReadOnlyMemory<byte>.Empty), cancellationToken);
	}
	
	async ValueTask IMessageReplySender.SendReply<TReply>(uint replyingToMessageId, TReply reply, CancellationToken cancellationToken) {
		var frame = new MessageReplyFrame(replyingToMessageId, MessageSerialization.Serialize(reply));
		logger.Debug("Sending reply to message {MessageId} ({ReplyBytes} B).", replyingToMessageId, frame.SerializedReply.Length);
		await SendFrame(frame, cancellationToken);
	}
	
	async ValueTask IMessageReplySender.SendError(uint replyingToMessageId, MessageError error, CancellationToken cancellationToken) {
		logger.Debug("Sending error response to message {MessageId}: {Error}", replyingToMessageId, error);
		await SendFrame(new MessageErrorFrame(replyingToMessageId, error), cancellationToken);
	}
	
	private async ValueTask SendFrame(IFrame frame, CancellationToken cancellationToken) {
		if (!Volatile.Read(ref isFrameQueueWritable)) {
			throw new ChannelClosedException();
		}
		
		if (!frameQueue.Writer.TryWrite(frame)) {
			logger.Warning("Queue is full, waiting to send next frame.");
			await frameQueue.Writer.WriteAsync(frame, cancellationToken);
		}
	}
	
	private async Task ProcessQueue() {
		CancellationToken cancellationToken = frameQueueCancellationTokenSource.Token;
		
		await foreach (IFrame frame in frameQueue.Reader.ReadAllAsync(cancellationToken)) {
			while (true) {
				try {
					RpcStream stream = await connectionProvider.GetStream(cancellationToken);
					await stream.WriteBytes(frame.FrameType, cancellationToken);
					await frame.Write(stream, cancellationToken);
					await stream.Flush(cancellationToken);
					break;
				} catch (OperationCanceledException) {
					throw;
				} catch (Exception) {
					// Retry.
				}
			}
		}
	}
	
	private async Task PingSchedule() {
		CancellationToken cancellationToken = pingCancellationTokenSource.Token;
		
		while (cancellationToken.Check()) {
			await Task.Delay(PingInterval, cancellationToken);
			
			pongTask = new TaskCompletionSource<DateTimeOffset>();
			
			if (!frameQueue.Writer.TryWrite(PingFrame.Instance)) {
				cancellationToken.ThrowIfCancellationRequested();
				logger.Warning("Skipped a ping due to a full queue.");
				continue;
			}
			
			DateTimeOffset pingTime = await pongTask.Task.WaitAsync(cancellationToken);
			DateTimeOffset currentTime = DateTimeOffset.UtcNow;
			
			if (logger.IsEnabled(LogEventLevel.Verbose)) {
				TimeSpan roundTripTime = currentTime - pingTime;
				logger.Verbose("Received pong, round trip time: {RoundTripTime} ms", (long) roundTripTime.TotalMilliseconds);
			}
		}
	}
	
	public bool ReceiveMessage(MessageFrame frame) {
		return messageReceiveTracker.ReceiveMessage(frame.MessageId);
	}
	
	public void ReceivePong(PongFrame frame) {
		pongTask?.TrySetResult(frame.PingTime);
	}
	
	public async Task Shutdown(bool sendSessionTermination) {
		await pingCancellationTokenSource.CancelAsync();
		
		CloseFrameQueueWriter(sendSessionTermination);
		
		try {
			await frameQueueTask.WaitAsync(TimeSpan.FromSeconds(15));
		} catch (TimeoutException) {
			logger.Warning("Could not finish processing frame queue before timeout, forcibly shutting it down.");
			await frameQueueCancellationTokenSource.CancelAsync();
		} catch (Exception) {
			// Ignore.
		}
		
		await WaitForTasksAndDispose();
	}
	
	public async Task ShutdownNow() {
		await pingCancellationTokenSource.CancelAsync();
		await frameQueueCancellationTokenSource.CancelAsync();
		
		CloseFrameQueueWriter(sendSessionTermination: false);
		
		await WaitForTasksAndDispose();
	}
	
	private void CloseFrameQueueWriter(bool sendSessionTermination) {
		Volatile.Write(ref isFrameQueueWritable, value: false);
		
		if (sendSessionTermination && !frameQueue.Writer.TryWrite(SessionTerminationFrame.Instance)) {
			logger.Warning("Could not enqueue session termination frame, queue is full.");
		}
		
		frameQueue.Writer.TryComplete();
	}
	
	private async Task WaitForTasksAndDispose() {
		await pingTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
		await frameQueueTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
		
		pingTask.Dispose();
		frameQueueTask.Dispose();
		
		frameQueueCancellationTokenSource.Dispose();
		pingCancellationTokenSource.Dispose();
	}
}
