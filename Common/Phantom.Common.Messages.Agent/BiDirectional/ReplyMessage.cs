using MemoryPack;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Agent.BiDirectional;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ReplyMessage(
	[property: MemoryPackOrder(0)] uint SequenceId,
	[property: MemoryPackOrder(1)] byte[] SerializedReply
) : IMessageToController, IMessageToAgent, IReply {
	private static readonly MessageQueueKey MessageQueueKey = new ("Reply");
	
	[MemoryPackIgnore]
	public MessageQueueKey QueueKey => MessageQueueKey;
	
	public Task<NoReply> Accept(IMessageToControllerListener listener) {
		return listener.HandleReply(this);
	}

	public Task<NoReply> Accept(IMessageToAgentListener listener) {
		return listener.HandleReply(this);
	}
}
