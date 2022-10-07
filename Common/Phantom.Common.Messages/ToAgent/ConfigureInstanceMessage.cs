using MessagePack;
using Phantom.Common.Data.Instance;

namespace Phantom.Common.Messages.ToAgent;

[MessagePackObject]
public sealed record ConfigureInstanceMessage(
	[property: Key(0)] uint SequenceId,
	[property: Key(1)] InstanceConfiguration Configuration
) : IMessageToAgent, IMessageWithReply {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleConfigureInstance(this);
	}
}
