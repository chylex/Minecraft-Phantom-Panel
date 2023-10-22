using System.Text.Json;
using Phantom.Controller.Database.Enums;

namespace Phantom.Controller.Services.Audit;

public sealed record AuditLogItem(DateTime UtcTime, Guid? UserGuid, string? UserName, AuditLogEventType EventType, AuditLogSubjectType SubjectType, string? SubjectId, JsonDocument? Data);
