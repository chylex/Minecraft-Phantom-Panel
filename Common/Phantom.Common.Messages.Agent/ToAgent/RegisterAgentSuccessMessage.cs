using System.Collections.Immutable;
using MemoryPack;

namespace Phantom.Common.Messages.Agent.ToAgent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record RegisterAgentSuccessMessage(
	[property: MemoryPackOrder(0)] ImmutableArray<ConfigureInstanceMessage> InitialInstanceConfigurations
) : IMessageToAgent;
