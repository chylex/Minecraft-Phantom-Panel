using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Phantom.Common.Data.Web.AuditLog;

namespace Phantom.Controller.Database.Entities;

[Table("AuditLog", Schema = "system")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public class AuditLogEntity : IDisposable {
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public long Id { get; init; }

	public Guid? UserGuid { get; init; }
	public DateTime UtcTime { get; init; } // Note: Converting to UTC is not best practice, but for historical records it's good enough.
	public AuditLogEventType EventType { get; init; }
	public AuditLogSubjectType SubjectType { get; init; }
	public string SubjectId { get; init; }
	public JsonDocument? Data { get; init; }

	public virtual UserEntity? User { get; init; }
	
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	internal AuditLogEntity() {
		SubjectId = string.Empty;
	}

	public AuditLogEntity(Guid? userGuid, AuditLogEventType eventType, string subjectId, Dictionary<string, object?>? data) {
		UserGuid = userGuid;
		UtcTime = DateTime.UtcNow;
		EventType = eventType;
		SubjectType = eventType.GetSubjectType();
		SubjectId = subjectId;
		Data = data == null ? null : JsonSerializer.SerializeToDocument(data);
	}

	public void Dispose() {
		Data?.Dispose();
	}
}
