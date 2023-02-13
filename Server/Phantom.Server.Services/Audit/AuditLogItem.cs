using System.Text.Json;
using Phantom.Server.Database.Enums;

namespace Phantom.Server.Services.Audit; 

public sealed record AuditLogItem(DateTime UtcTime, string? UserId, string? UserName, AuditLogEventType EventType, AuditLogSubjectType SubjectType, string? SubjectId, JsonDocument? Data);
