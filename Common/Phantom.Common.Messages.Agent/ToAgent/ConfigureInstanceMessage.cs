using MemoryPack;
using Phantom.Common.Data;
using Phantom.Common.Data.Agent.Instance;
using Phantom.Common.Data.Agent.Instance.Launch;
using Phantom.Common.Data.Agent.Instance.Stop;
using Phantom.Common.Data.Replies;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Agent.ToAgent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ConfigureInstanceMessage(
	[property: MemoryPackOrder(0)] Guid InstanceGuid,
	[property: MemoryPackOrder(1)] InstanceInfo Info,
	[property: MemoryPackOrder(2)] InstanceLaunchRecipe? LaunchRecipe,
	[property: MemoryPackOrder(3)] bool LaunchNow,
	[property: MemoryPackOrder(4)] InstanceStopRecipe StopRecipe
) : IMessageToAgent, ICanReply<Result<ConfigureInstanceResult, InstanceActionFailure>>;
