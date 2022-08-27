using MessagePack;

namespace Phantom.Common.Messages.ToServer;

[MessagePackObject]
public sealed record InstanceOutputLineMessage(
	[property: Key(0)] Guid InstanceGuid,
	[property: Key(1)] string Line
) : IMessageToServer {
	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleInstanceOutputLine(this);
	}
}
