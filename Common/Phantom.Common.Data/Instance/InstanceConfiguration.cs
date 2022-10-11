using System.Collections.Immutable;
using MessagePack;
using Phantom.Common.Data.Minecraft;

namespace Phantom.Common.Data.Instance;

[MessagePackObject]
public sealed record InstanceConfiguration(
	[property: Key(0)] Guid AgentGuid,
	[property: Key(1)] Guid InstanceGuid,
	[property: Key(2)] string InstanceName,
	[property: Key(3)] ushort ServerPort,
	[property: Key(4)] ushort RconPort,
	[property: Key(5)] string MinecraftVersion,
	[property: Key(6)] MinecraftServerKind MinecraftServerKind,
	[property: Key(7)] RamAllocationUnits MemoryAllocation,
	[property: Key(8)] Guid JavaRuntimeGuid,
	[property: Key(9)] ImmutableArray<string> JvmArguments,
	[property: Key(10)] bool LaunchAutomatically
);
