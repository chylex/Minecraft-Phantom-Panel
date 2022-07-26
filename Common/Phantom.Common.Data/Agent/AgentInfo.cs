﻿using MemoryPack;

namespace Phantom.Common.Data.Agent;

[MemoryPackable]
public sealed partial record AgentInfo(
	[property: MemoryPackOrder(0)] Guid Guid,
	[property: MemoryPackOrder(1)] string Name,
	[property: MemoryPackOrder(2)] ushort ProtocolVersion,
	[property: MemoryPackOrder(3)] string BuildVersion,
	[property: MemoryPackOrder(4)] ushort MaxInstances,
	[property: MemoryPackOrder(5)] RamAllocationUnits MaxMemory,
	[property: MemoryPackOrder(6)] AllowedPorts AllowedServerPorts,
	[property: MemoryPackOrder(7)] AllowedPorts AllowedRconPorts
);
