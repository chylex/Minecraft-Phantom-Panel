using System.Text.Json;
using Phantom.Server.Database.Enums;

namespace Phantom.Server.Services.Audit; 

public sealed record AuditEvent(DateTime UtcTime, string? UserId, string? UserName, AuditEventType EventType, AuditSubjectType SubjectType, string? SubjectId, JsonDocument? Data);
