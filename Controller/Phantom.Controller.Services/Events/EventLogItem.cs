using System.Text.Json;
using Phantom.Controller.Database.Enums;

namespace Phantom.Controller.Services.Events;

public sealed record EventLogItem(DateTime UtcTime, Guid? AgentGuid, EventLogEventType EventType, EventLogSubjectType SubjectType, string SubjectId, JsonDocument? Data);
