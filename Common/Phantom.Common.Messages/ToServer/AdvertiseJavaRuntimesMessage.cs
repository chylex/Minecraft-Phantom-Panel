using System.Collections.Immutable;
using MessagePack;
using Phantom.Common.Data.Java;

namespace Phantom.Common.Messages.ToServer; 

[MessagePackObject]
public sealed record AdvertiseJavaRuntimesMessage(
	[property: Key(0)] ImmutableArray<TaggedJavaRuntime> Runtimes
) : IMessageToServer {
	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleAdvertiseJavaRuntimes(this);
	}
}
