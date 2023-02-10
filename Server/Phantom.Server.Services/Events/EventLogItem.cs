using System.Text.Json;
using Phantom.Server.Database.Enums;

namespace Phantom.Server.Services.Events; 

public sealed record EventLogItem(DateTime UtcTime, Guid? AgentGuid, EventLogEventType EventType, EventLogSubjectType SubjectType, string SubjectId, JsonDocument? Data);
