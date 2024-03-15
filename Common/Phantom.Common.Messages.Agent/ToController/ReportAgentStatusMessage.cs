using MemoryPack;
using Phantom.Common.Data;

namespace Phantom.Common.Messages.Agent.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ReportAgentStatusMessage(
	[property: MemoryPackOrder(0)] int RunningInstanceCount,
	[property: MemoryPackOrder(1)] RamAllocationUnits RunningInstanceMemory
) : IMessageToController;
