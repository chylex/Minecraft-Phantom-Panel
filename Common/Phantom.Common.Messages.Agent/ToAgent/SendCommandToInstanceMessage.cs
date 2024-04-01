using MemoryPack;
using Phantom.Common.Data;
using Phantom.Common.Data.Replies;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Agent.ToAgent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record SendCommandToInstanceMessage(
	[property: MemoryPackOrder(0)] Guid InstanceGuid,
	[property: MemoryPackOrder(1)] string Command
) : IMessageToAgent, ICanReply<Result<SendCommandToInstanceResult, InstanceActionFailure>>;
