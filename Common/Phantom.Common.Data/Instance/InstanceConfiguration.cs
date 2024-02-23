using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Minecraft;

namespace Phantom.Common.Data.Instance;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceConfiguration(
	[property: MemoryPackOrder(0)] Guid AgentGuid,
	[property: MemoryPackOrder(1)] string InstanceName,
	[property: MemoryPackOrder(2)] ushort ServerPort,
	[property: MemoryPackOrder(3)] ushort RconPort,
	[property: MemoryPackOrder(4)] string MinecraftVersion,
	[property: MemoryPackOrder(5)] MinecraftServerKind MinecraftServerKind,
	[property: MemoryPackOrder(6)] RamAllocationUnits MemoryAllocation,
	[property: MemoryPackOrder(7)] Guid JavaRuntimeGuid,
	[property: MemoryPackOrder(8)] ImmutableArray<string> JvmArguments
);
