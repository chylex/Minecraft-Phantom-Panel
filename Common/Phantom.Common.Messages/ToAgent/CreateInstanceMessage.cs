using MessagePack;
using Phantom.Common.Data;

namespace Phantom.Common.Messages.ToAgent;

[MessagePackObject]
public sealed record CreateInstanceMessage(
	[property: Key(0)] InstanceInfo Instance
) : IMessageToAgent {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleCreateInstance(this);
	}
}
