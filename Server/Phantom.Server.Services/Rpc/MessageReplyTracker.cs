using System.Collections.Concurrent;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToServer;
using Serilog;

namespace Phantom.Server.Services.Rpc; 

sealed class MessageReplyTracker {
	private static readonly ILogger Logger = PhantomLogger.Create<MessageReplyTracker>();
	
	private uint lastSequenceId;
	private readonly ConcurrentDictionary<uint, Func<int, Task>> simpleReplyCallbacks = new (4, 16);
	private readonly ConcurrentDictionary<uint, int> simpleReplyResults = new (3, 8);

	public (uint, ManualResetEventSlim) RegisterSimpleReplyCallback() {
		var sequenceId = Interlocked.Increment(ref lastSequenceId);
		var resetEvent = new ManualResetEventSlim(false);
		
		simpleReplyCallbacks[sequenceId] = result => {
			simpleReplyResults[sequenceId] = result;
			resetEvent.Set();
			return Task.CompletedTask;
		};
		
		return (sequenceId, resetEvent);
	}

	public int? GetSimpleReplyResult(uint sequenceId) {
		RemoveSimpleReplyCallback(sequenceId);
		
		if (!simpleReplyResults.TryRemove(sequenceId, out var result)) {
			Logger.Warning("Requested a reply result with id {SequenceId} but no result was found.", sequenceId);
			return null;
		}
		
		return result;
	}

	public void RemoveSimpleReplyCallback(uint sequenceId) {
		simpleReplyCallbacks.TryRemove(sequenceId, out _);
	}

	public Task ReceiveSimpleReply(SimpleReplyMessage message) {
		if (simpleReplyCallbacks.TryRemove(message.SequenceId, out var callback)) {
			return callback(message.EnumValue);
		}
		else {
			Logger.Warning("Received a reply with id {SequenceId} but no registered callback.", message.SequenceId);
			return Task.CompletedTask;
		}
	}
}
