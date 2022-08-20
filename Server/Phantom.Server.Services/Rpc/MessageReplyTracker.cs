using System.Collections.Concurrent;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToServer;
using Serilog;

namespace Phantom.Server.Services.Rpc; 

sealed class MessageReplyTracker {
	private static readonly ILogger Logger = PhantomLogger.Create<MessageReplyTracker>();
	
	private readonly ConcurrentDictionary<uint, Func<int, Task>> simpleReplyCallbacks = new (4, 16);
	
	public Task Receive(SimpleReplyMessage message) {
		if (simpleReplyCallbacks.TryRemove(message.SequenceId, out var callback)) {
			return callback(message.EnumValue);
		}
		else {
			Logger.Warning("Received a reply with id {SequenceId} but no callback.", message.SequenceId);
			return Task.CompletedTask;
		}
	}
}
