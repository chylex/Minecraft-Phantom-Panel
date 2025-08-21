using System.Collections.Concurrent;
using Phantom.Utils.Logging;
using Phantom.Utils.Tasks;
using Serilog;

namespace Phantom.Utils.Rpc.Message;

sealed class MessageReplyTracker {
	private readonly ILogger logger;
	private readonly ConcurrentDictionary<uint, TaskCompletionSource<byte[]>> replyTasks = new (4, 16);
	
	private uint lastSequenceId;
	
	internal MessageReplyTracker(string loggerName) {
		this.logger = PhantomLogger.Create<MessageReplyTracker>(loggerName);
	}
	
	public uint RegisterReply() {
		var sequenceId = Interlocked.Increment(ref lastSequenceId);
		replyTasks[sequenceId] = AsyncTasks.CreateCompletionSource<byte[]>();
		return sequenceId;
	}
	
	public async Task<TReply> WaitForReply<TReply>(uint sequenceId, TimeSpan waitForReplyTime, CancellationToken cancellationToken) {
		if (!replyTasks.TryGetValue(sequenceId, out var completionSource)) {
			logger.Warning("No reply callback for id {SequenceId}.", sequenceId);
			throw new ArgumentException("No reply callback for id: " + sequenceId, nameof(sequenceId));
		}
		
		try {
			byte[] replyBytes = await completionSource.Task.WaitAsync(waitForReplyTime, cancellationToken);
			return MessageSerializer.Deserialize<TReply>(replyBytes);
		} catch (TimeoutException) {
			logger.Debug("Timed out waiting for reply with id {SequenceId}.", sequenceId);
			throw;
		} catch (OperationCanceledException) {
			logger.Debug("Cancelled waiting for reply with id {SequenceId}.", sequenceId);
			throw;
		} catch (Exception e) {
			logger.Warning(e, "Error processing reply with id {SequenceId}.", sequenceId);
			throw;
		} finally {
			ForgetReply(sequenceId);
		}
	}
	
	public void ForgetReply(uint sequenceId) {
		if (replyTasks.TryRemove(sequenceId, out var task)) {
			task.SetCanceled();
		}
	}
	
	public void ReceiveReply(uint sequenceId, byte[] serializedReply) {
		if (replyTasks.TryRemove(sequenceId, out var task)) {
			task.SetResult(serializedReply);
		}
		else {
			logger.Warning("Received a reply with id {SequenceId} but no registered callback.", sequenceId);
		}
	}
}
