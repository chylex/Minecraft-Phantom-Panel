using System.Collections.Immutable;
using MessagePack;

namespace Phantom.Common.Messages.ToServer;

[MessagePackObject]
public sealed record InstanceOutputMessage(
	[property: Key(0)] Guid InstanceGuid,
	[property: Key(1)] ImmutableArray<string> Lines
) : IMessageToServer {
	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleInstanceOutput(this);
	}
}
