using System.Collections.Immutable;
using MemoryPack;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Agent.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceOutputMessage(
	[property: MemoryPackOrder(0)] Guid InstanceGuid,
	[property: MemoryPackOrder(1)] ImmutableArray<string> Lines
) : IMessageToController {
	private static readonly MessageQueueKey MessageQueueKey = new ("Agent.InstanceOutput");
	
	[MemoryPackIgnore]
	public MessageQueueKey QueueKey => MessageQueueKey;

	public Task<NoReply> Accept(IMessageToControllerListener listener) {
		return listener.HandleInstanceOutput(this);
	}
}
