using MemoryPack;
using Phantom.Common.Data;
using Phantom.Common.Data.Replies;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Web.ToController; 

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record LaunchInstanceMessage(
	[property: MemoryPackOrder(0)] Guid LoggedInUserGuid,
	[property: MemoryPackOrder(1)] Guid AgentGuid,
	[property: MemoryPackOrder(2)] Guid InstanceGuid
) : IMessageToController, ICanReply<Result<LaunchInstanceResult, InstanceActionFailure>>;
