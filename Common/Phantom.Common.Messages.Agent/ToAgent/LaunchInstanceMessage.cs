using MemoryPack;
using Phantom.Common.Data;
using Phantom.Common.Data.Replies;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Agent.ToAgent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record LaunchInstanceMessage(
	[property: MemoryPackOrder(0)] Guid InstanceGuid
) : IMessageToAgent, ICanReply<Result<LaunchInstanceResult, InstanceActionFailure>>;
