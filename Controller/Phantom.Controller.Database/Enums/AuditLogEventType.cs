namespace Phantom.Server.Database.Enums;

public enum AuditLogEventType {
	AdministratorUserCreated,
	AdministratorUserModified,
	UserLoggedIn,
	UserLoggedOut,
	UserCreated,
	UserRolesChanged,
	UserDeleted,
	InstanceCreated,
	InstanceEdited,
	InstanceLaunched,
	InstanceStopped,
	InstanceCommandExecuted
}

static class AuditLogEventTypeExtensions {
	private static readonly Dictionary<AuditLogEventType, AuditLogSubjectType> SubjectTypes = new () {
		{ AuditLogEventType.AdministratorUserCreated,  AuditLogSubjectType.User },
		{ AuditLogEventType.AdministratorUserModified, AuditLogSubjectType.User },
		{ AuditLogEventType.UserLoggedIn,              AuditLogSubjectType.User },
		{ AuditLogEventType.UserLoggedOut,             AuditLogSubjectType.User },
		{ AuditLogEventType.UserCreated,               AuditLogSubjectType.User },
		{ AuditLogEventType.UserRolesChanged,          AuditLogSubjectType.User },
		{ AuditLogEventType.UserDeleted,               AuditLogSubjectType.User },
		{ AuditLogEventType.InstanceCreated,           AuditLogSubjectType.Instance },
		{ AuditLogEventType.InstanceEdited,            AuditLogSubjectType.Instance },
		{ AuditLogEventType.InstanceLaunched,          AuditLogSubjectType.Instance },
		{ AuditLogEventType.InstanceStopped,           AuditLogSubjectType.Instance },
		{ AuditLogEventType.InstanceCommandExecuted,   AuditLogSubjectType.Instance }
	};

	static AuditLogEventTypeExtensions() {
		foreach (var eventType in Enum.GetValues<AuditLogEventType>()) {
			if (!SubjectTypes.ContainsKey(eventType)) {
				throw new Exception("Missing mapping from " + eventType + " to a subject type.");
			}
		}
	}

	internal static AuditLogSubjectType GetSubjectType(this AuditLogEventType type) {
		return SubjectTypes[type];
	}
}
