using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Phantom.Server.Database.Enums;

namespace Phantom.Server.Database.Entities; 

[Table("AuditEvents", Schema = "system")]
public class AuditEventEntity : IDisposable {
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public long Id { get; set; }

	public string? UserId { get; set; }
	public DateTime UtcTime { get; set; } // Note: Converting to UTC is not best practice, but for historical records it's good enough.
	public AuditEventType EventType { get; set; }
	public AuditSubjectType SubjectType { get; set; }
	public string SubjectId { get; set; }
	public JsonDocument? Data { get; set; }

	public virtual IdentityUser? User { get; set; }
	
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	internal AuditEventEntity() {
		SubjectId = string.Empty;
	}

	public AuditEventEntity(string? userId, AuditEventType eventType, string subjectId, Dictionary<string, object?>? data) {
		UserId = userId;
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
