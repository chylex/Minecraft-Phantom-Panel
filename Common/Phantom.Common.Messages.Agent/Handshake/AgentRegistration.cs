using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Java;

namespace Phantom.Common.Messages.Agent.Handshake;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AgentRegistration(
	[property: MemoryPackOrder(0)] AgentInfo AgentInfo,
	[property: MemoryPackOrder(1)] ImmutableArray<TaggedJavaRuntime> JavaRuntimes
);
