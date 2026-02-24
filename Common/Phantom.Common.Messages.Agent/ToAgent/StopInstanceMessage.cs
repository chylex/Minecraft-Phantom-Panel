using MemoryPack;
using Phantom.Common.Data;
using Phantom.Common.Data.Agent.Instance.Stop;
using Phantom.Common.Data.Replies;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Agent.ToAgent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record StopInstanceMessage(
	[property: MemoryPackOrder(0)] Guid InstanceGuid,
	[property: MemoryPackOrder(1)] InstanceStopRecipe StopRecipe
) : IMessageToAgent, ICanReply<Result<StopInstanceResult, InstanceActionFailure>>;
