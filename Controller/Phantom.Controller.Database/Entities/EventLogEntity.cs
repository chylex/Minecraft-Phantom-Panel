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
	public Guid EventGuid { get; set; }

	public DateTime UtcTime { get; set; } // Note: Converting to UTC is not best practice, but for historical records it's good enough.
	public Guid? AgentGuid { get; set; }
	public EventLogEventType EventType { get; set; }
	public EventLogSubjectType SubjectType { get; set; }
	public string SubjectId { get; set; }
	public JsonDocument? Data { get; set; }
	
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
