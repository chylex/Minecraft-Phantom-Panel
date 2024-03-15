using MemoryPack;
using Phantom.Common.Data.Replies;

namespace Phantom.Common.Messages.Agent.ToAgent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record RegisterAgentFailureMessage(
	[property: MemoryPackOrder(0)] RegisterAgentFailure FailureKind
) : IMessageToAgent;
