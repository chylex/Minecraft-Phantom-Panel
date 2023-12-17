using System.Collections.Immutable;
using MemoryPack;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Web.ToWeb;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceOutputMessage(
	[property: MemoryPackOrder(0)] Guid InstanceGuid,
	[property: MemoryPackOrder(1)] ImmutableArray<string> Lines
) : IMessageToWeb {
	public Task<NoReply> Accept(IMessageToWebListener listener) {
		return listener.HandleInstanceOutput(this);
	}
}