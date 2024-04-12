using MemoryPack;
using Phantom.Common.Data.Instance;

namespace Phantom.Common.Messages.Agent.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ReportInstancePlayerCountsMessage(
	[property: MemoryPackOrder(0)] Guid InstanceGuid,
	[property: MemoryPackOrder(1)] InstancePlayerCounts? PlayerCounts
) : IMessageToController;
