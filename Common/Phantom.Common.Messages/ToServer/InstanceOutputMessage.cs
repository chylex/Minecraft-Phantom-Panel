using System.Collections.Immutable;
using MemoryPack;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.ToServer;

[MemoryPackable]
public sealed partial record InstanceOutputMessage(
	[property: MemoryPackOrder(0)] Guid InstanceGuid,
	[property: MemoryPackOrder(1)] ImmutableArray<string> Lines
) : IMessageToServer {
	public Task<NoReply> Accept(IMessageToServerListener listener) {
		return listener.HandleInstanceOutput(this);
	}
}
