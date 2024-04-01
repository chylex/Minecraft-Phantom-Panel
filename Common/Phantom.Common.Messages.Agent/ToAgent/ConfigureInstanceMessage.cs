using MemoryPack;
using Phantom.Common.Data;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Agent.ToAgent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ConfigureInstanceMessage(
	[property: MemoryPackOrder(0)] Guid InstanceGuid,
	[property: MemoryPackOrder(1)] InstanceConfiguration Configuration,
	[property: MemoryPackOrder(2)] InstanceLaunchProperties LaunchProperties,
	[property: MemoryPackOrder(3)] bool LaunchNow = false
) : IMessageToAgent, ICanReply<Result<ConfigureInstanceResult, InstanceActionFailure>>;
