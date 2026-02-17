using System.Collections.Immutable;
using MemoryPack;

namespace Phantom.Common.Data.Agent.Instance.Launch;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceLaunchRecipe(
	[property: MemoryPackOrder(0)] ImmutableArray<IInstanceLaunchStep> Preparation,
	[property: MemoryPackOrder(1)] IInstancePath Executable,
	[property: MemoryPackOrder(2)] ImmutableArray<IInstanceValue> Arguments
);
