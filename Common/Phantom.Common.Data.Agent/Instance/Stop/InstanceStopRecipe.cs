using System.Collections.Immutable;
using MemoryPack;

namespace Phantom.Common.Data.Agent.Instance.Stop;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceStopRecipe(
	[property: MemoryPackOrder(0)] ImmutableArray<IInstanceStopStep> Preparation,
	[property: MemoryPackOrder(1)] IInstanceValue StopCommand
);
