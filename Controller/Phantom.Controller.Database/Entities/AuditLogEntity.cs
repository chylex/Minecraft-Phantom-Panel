﻿using System.ComponentModel.DataAnnotations;
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
	public long Id { get; set; }

	public Guid? UserGuid { get; set; }
	public DateTime UtcTime { get; set; } // Note: Converting to UTC is not best practice, but for historical records it's good enough.
	public AuditLogEventType EventType { get; set; }
	public AuditLogSubjectType SubjectType { get; set; }
	public string SubjectId { get; set; }
	public JsonDocument? Data { get; set; }

	public virtual UserEntity? User { get; set; }
	
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
