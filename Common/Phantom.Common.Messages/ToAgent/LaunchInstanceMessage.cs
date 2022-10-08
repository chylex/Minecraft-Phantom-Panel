﻿using MessagePack;

namespace Phantom.Common.Messages.ToAgent; 

[MessagePackObject]
public sealed record LaunchInstanceMessage(
	[property: Key(0)] uint SequenceId,
	[property: Key(1)] Guid InstanceGuid
) : IMessageToAgent, IMessageWithReply {
	public Task Accept(IMessageToAgentListener listener) {
		return listener.HandleLaunchInstance(this);
	}
}