using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Phantom.Common.Data.Web.EventLog;

namespace Phantom.Controller.Database.Entities;

[Table("EventLog", Schema = "system")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public sealed class EventLogEntity : IDisposable {
	[Key]
	public Guid EventGuid { get; init; }
	
	public DateTime UtcTime { get; init; } // Note: Converting to UTC is not best practice, but for historical records it's good enough.
	public Guid? AgentGuid { get; init; }
	public EventLogEventType EventType { get; init; }
	public EventLogSubjectType SubjectType { get; init; }
	public string SubjectId { get; init; }
	public JsonDocument? Data { get; init; }
	
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	internal EventLogEntity() {
		SubjectId = string.Empty;
	}
	
	public EventLogEntity(Guid eventGuid, DateTime utcTime, Guid? agentGuid, EventLogEventType eventType, string subjectId, Dictionary<string, object?>? data) {
		EventGuid = eventGuid;
		UtcTime = utcTime;
		AgentGuid = agentGuid;
		EventType = eventType;
		SubjectType = eventType.GetSubjectType();
		SubjectId = subjectId;
		Data = data == null ? null : JsonSerializer.SerializeToDocument(data);
	}
	
	public void Dispose() {
		Data?.Dispose();
	}
}
