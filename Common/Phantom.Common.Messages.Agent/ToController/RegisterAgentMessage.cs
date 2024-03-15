using MemoryPack;
using Phantom.Common.Data;
using Phantom.Common.Data.Agent;

namespace Phantom.Common.Messages.Agent.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record RegisterAgentMessage(
	[property: MemoryPackOrder(0)] AuthToken AuthToken,
	[property: MemoryPackOrder(1)] AgentInfo AgentInfo
) : IMessageToController;
