using MemoryPack;
using Phantom.Common.Data.Instance;

namespace Phantom.Common.Messages.Agent.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ReportInstanceEventMessage(
	[property: MemoryPackOrder(0)] Guid EventGuid,
	[property: MemoryPackOrder(1)] DateTime UtcTime,
	[property: MemoryPackOrder(2)] Guid InstanceGuid,
	[property: MemoryPackOrder(3)] IInstanceEvent Event
) : IMessageToController;
