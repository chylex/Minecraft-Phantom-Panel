using MemoryPack;

namespace Phantom.Common.Data.Web.AuditLog;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AuditLogItem(
	[property: MemoryPackOrder(0)] DateTime UtcTime,
	[property: MemoryPackOrder(1)] Guid? UserGuid,
	[property: MemoryPackOrder(2)] string? UserName,
	[property: MemoryPackOrder(3)] AuditLogEventType EventType,
	[property: MemoryPackOrder(4)] AuditLogSubjectType SubjectType,
	[property: MemoryPackOrder(5)] string? SubjectId,
	[property: MemoryPackOrder(6)] string? JsonData
);
