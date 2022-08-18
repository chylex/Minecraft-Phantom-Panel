using MessagePack;
using Phantom.Common.Data;
using Phantom.Common.Rpc.Message;

namespace Phantom.Common.Rpc.Messages.ToAgent;

[MessagePackObject]
public sealed record CreateInstanceMessage(
	[property: Key(0)] InstanceInfo Instance
) : IMessageToAgent {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleCreateInstance(this);
	}
}
