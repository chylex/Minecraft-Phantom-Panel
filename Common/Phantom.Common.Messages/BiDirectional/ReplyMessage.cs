using MemoryPack;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.BiDirectional;

[MemoryPackable]
public sealed partial record ReplyMessage(
	[property: MemoryPackOrder(0)] uint SequenceId,
	[property: MemoryPackOrder(1)] byte[] SerializedReply
) : IMessageToServer, IMessageToAgent {
	public Task<NoReply> Accept(IMessageToServerListener listener) {
		return listener.HandleReply(this);
	}

	public Task<NoReply> Accept(IMessageToAgentListener listener) {
		return listener.HandleReply(this);
	}
}
