namespace Phantom.Controller.Database.Enums;

public enum EventLogEventType {
	InstanceLaunchSucceded,
	InstanceLaunchFailed,
	InstanceCrashed,
	InstanceStopped,
	InstanceBackupSucceeded,
	InstanceBackupSucceededWithWarnings,
	InstanceBackupFailed,
}

static class EventLogEventTypeExtensions {
	private static readonly Dictionary<EventLogEventType, EventLogSubjectType> SubjectTypes = new () {
		{ EventLogEventType.InstanceLaunchSucceded, EventLogSubjectType.Instance },
		{ EventLogEventType.InstanceLaunchFailed, EventLogSubjectType.Instance },
		{ EventLogEventType.InstanceCrashed, EventLogSubjectType.Instance },
		{ EventLogEventType.InstanceStopped, EventLogSubjectType.Instance },
		{ EventLogEventType.InstanceBackupSucceeded, EventLogSubjectType.Instance },
		{ EventLogEventType.InstanceBackupSucceededWithWarnings, EventLogSubjectType.Instance },
		{ EventLogEventType.InstanceBackupFailed, EventLogSubjectType.Instance },
	};

	static EventLogEventTypeExtensions() {
		foreach (var eventType in Enum.GetValues<EventLogEventType>()) {
			if (!SubjectTypes.ContainsKey(eventType)) {
				throw new Exception("Missing mapping from " + eventType + " to a subject type.");
			}
		}
	}

	public static EventLogSubjectType GetSubjectType(this EventLogEventType type) {
		return SubjectTypes[type];
	}
}
