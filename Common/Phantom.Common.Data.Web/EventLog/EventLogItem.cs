using MemoryPack;

namespace Phantom.Common.Data.Web.EventLog;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record EventLogItem(
	[property: MemoryPackOrder(0)] DateTime UtcTime,
	[property: MemoryPackOrder(1)] Guid? AgentGuid,
	[property: MemoryPackOrder(2)] EventLogEventType EventType,
	[property: MemoryPackOrder(3)] EventLogSubjectType SubjectType,
	[property: MemoryPackOrder(4)] string SubjectId,
	[property: MemoryPackOrder(5)] string? JsonData
);
