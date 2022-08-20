using MessagePack;
using Phantom.Common.Data;

namespace Phantom.Common.Messages.ToAgent;

[MessagePackObject]
public sealed record CreateInstanceMessage(
	[property: Key(0)] uint SequenceId,
	[property: Key(1)] InstanceInfo Instance
) : IMessageToAgent, IMessageWithReply {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleCreateInstance(this);
	}
}
