using System.Collections.Concurrent;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToServer;
using Serilog;

namespace Phantom.Server.Services.Rpc;

sealed class MessageReplyTracker {
	private static readonly ILogger Logger = PhantomLogger.Create<MessageReplyTracker>();

	public static MessageReplyTracker Instance { get; } = new ();
	
	private uint lastSequenceId;
	private readonly ConcurrentDictionary<uint, TaskCompletionSource<int?>> simpleReplyTasks = new (4, 16);
	
	private MessageReplyTracker() {}

	public uint RegisterReply() {
		var sequenceId = Interlocked.Increment(ref lastSequenceId);
		simpleReplyTasks[sequenceId] = new TaskCompletionSource<int?>(TaskCreationOptions.None);
		return sequenceId;
	}

	public async Task<int?> WaitForReply(uint sequenceId, TimeSpan waitForReplyTime, CancellationToken cancellationToken) {
		if (!simpleReplyTasks.TryGetValue(sequenceId, out var completionSource)) {
			Logger.Warning("No reply callback for id {SequenceId}.", sequenceId);
			return null;
		}
		
		try {
			return await completionSource.Task.WaitAsync(waitForReplyTime, cancellationToken);
		} catch (TimeoutException) {
			return null;
		} catch (OperationCanceledException) {
			return null;
		} catch (Exception e) {
			Logger.Warning(e, "Error processing reply with id {SequenceId}.", sequenceId);
			return null;
		} finally {
			ForgetReply(sequenceId);
		}
	}

	public void ForgetReply(uint sequenceId) {
		if (simpleReplyTasks.TryRemove(sequenceId, out var task)) {
			task.SetCanceled();
		}
	}

	public void ReceiveReply(SimpleReplyMessage message) {
		if (simpleReplyTasks.TryRemove(message.SequenceId, out var task)) {
			task.SetResult(message.EnumValue);
		}
		else {
			Logger.Warning("Received a reply with id {SequenceId} but no registered callback.", message.SequenceId);
		}
	}
}
