using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Java;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Agent.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AdvertiseJavaRuntimesMessage(
	[property: MemoryPackOrder(0)] ImmutableArray<TaggedJavaRuntime> Runtimes
) : IMessageToController {
	public Task<NoReply> Accept(IMessageToControllerListener listener) {
		return listener.HandleAdvertiseJavaRuntimes(this);
	}
}
