using System.Collections.Concurrent;
using System.Diagnostics;
using Phantom.Utils.Logging;
using Phantom.Utils.Tasks;
using Serilog;
using Serilog.Events;

namespace Phantom.Utils.Rpc.Message;

sealed class MessageReplyTracker {
	private readonly ILogger logger;
	private readonly ConcurrentDictionary<uint, Reply> pendingReplies = new (concurrencyLevel: 2, capacity: 16);
	
	internal MessageReplyTracker(string loggerName) {
		this.logger = PhantomLogger.Create<MessageReplyTracker>(loggerName);
	}
	
	public void RegisterReply<TMessage>(uint messageId) {
		pendingReplies[messageId] = Reply.Create(typeof(TMessage));
	}
	
	public async Task<TReply> WaitForReply<TReply>(uint messageId, TimeSpan waitForReplyTime, CancellationToken cancellationToken) {
		if (!pendingReplies.TryGetValue(messageId, out var reply)) {
			logger.Warning("No reply callback for message {MessageId}.", messageId);
			throw new ArgumentException("No reply callback for message: " + messageId, nameof(messageId));
		}
		
		try {
			ReadOnlyMemory<byte> serializedReply = await reply.Result.Task.WaitAsync(waitForReplyTime, cancellationToken);
			return MessageSerialization.Deserialize<TReply>(serializedReply);
		} catch (TimeoutException) {
			logger.Debug("Timed out waiting for reply with message {MessageId}.", messageId);
			throw;
		} catch (OperationCanceledException) {
			logger.Debug("Cancelled waiting for reply with message {MessageId}.", messageId);
			throw;
		} catch (Exception e) {
			logger.Warning(e, "Error processing reply with message {MessageId}.", messageId);
			throw;
		} finally {
			ForgetReply(messageId);
		}
	}
	
	private bool CompleteReply(uint messageId, out Reply reply) {
		if (pendingReplies.TryRemove(messageId, out reply)) {
			reply.Stopwatch.Stop();
			return true;
		}
		else {
			return false;
		}
	}
	
	public void ReceiveReply(uint messageId, ReadOnlyMemory<byte> serializedReply) {
		if (CompleteReply(messageId, out var reply)) {
			if (logger.IsEnabled(LogEventLevel.Debug)) {
				logger.Debug("Received reply to message {MessageId} of type {MessageType} in {WaitTime} ms ({ReplyBytes} B).", messageId, reply.MessageType.Name, reply.Stopwatch.ElapsedMilliseconds, serializedReply.Length);
			}
			
			reply.Result.SetResult(serializedReply);
		}
	}
	
	public void FailReply(uint messageId, MessageErrorException e) {
		if (CompleteReply(messageId, out var reply)) {
			if (logger.IsEnabled(LogEventLevel.Debug)) {
				logger.Debug("Received error response to message {MessageId} of type {MessageType} in {WaitTime} ms: {Error}", messageId, reply.MessageType.Name, reply.Stopwatch.ElapsedMilliseconds, e.Error);
			}
			
			reply.Result.SetException(e);
		}
	}
	
	public void ForgetReply(uint messageId) {
		if (CompleteReply(messageId, out var reply)) {
			if (logger.IsEnabled(LogEventLevel.Debug)) {
				logger.Debug("Cancelled reply to message {MessageId} of type {MessageType}.", messageId, reply.MessageType);
			}
			
			reply.Result.SetCanceled();
		}
	}
	
	private readonly record struct Reply(Type MessageType, TaskCompletionSource<ReadOnlyMemory<byte>> Result, Stopwatch Stopwatch) {
		public static Reply Create(Type messageType) {
			return new Reply(messageType, AsyncTasks.CreateCompletionSource<ReadOnlyMemory<byte>>(), Stopwatch.StartNew());
		}
	}
}
