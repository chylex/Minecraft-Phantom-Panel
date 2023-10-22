using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Java;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.ToServer;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AdvertiseJavaRuntimesMessage(
	[property: MemoryPackOrder(0)] ImmutableArray<TaggedJavaRuntime> Runtimes
) : IMessageToServer {
	public Task<NoReply> Accept(IMessageToServerListener listener) {
		return listener.HandleAdvertiseJavaRuntimes(this);
	}
}
