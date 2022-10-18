using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Java;

namespace Phantom.Common.Messages.ToServer; 

[MemoryPackable]
public sealed partial record AdvertiseJavaRuntimesMessage(
	[property: MemoryPackOrder(0)] ImmutableArray<TaggedJavaRuntime> Runtimes
) : IMessageToServer {
	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleAdvertiseJavaRuntimes(this);
	}
}
