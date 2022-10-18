using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Minecraft;

namespace Phantom.Common.Data.Instance;

[MemoryPackable]
public sealed partial record InstanceConfiguration(
	[property: MemoryPackOrder(0)] Guid AgentGuid,
	[property: MemoryPackOrder(1)] Guid InstanceGuid,
	[property: MemoryPackOrder(2)] string InstanceName,
	[property: MemoryPackOrder(3)] ushort ServerPort,
	[property: MemoryPackOrder(4)] ushort RconPort,
	[property: MemoryPackOrder(5)] string MinecraftVersion,
	[property: MemoryPackOrder(6)] MinecraftServerKind MinecraftServerKind,
	[property: MemoryPackOrder(7)] RamAllocationUnits MemoryAllocation,
	[property: MemoryPackOrder(8)] Guid JavaRuntimeGuid,
	[property: MemoryPackOrder(9)] ImmutableArray<string> JvmArguments,
	[property: MemoryPackOrder(10)] bool LaunchAutomatically
);
