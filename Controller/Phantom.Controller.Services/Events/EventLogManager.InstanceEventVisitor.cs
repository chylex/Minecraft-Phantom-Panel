using Phantom.Common.Data.Backups;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Web.EventLog;

namespace Phantom.Controller.Services.Events;

sealed partial class EventLogManager {
	internal IInstanceEventVisitor CreateInstanceEventVisitor(Guid eventGuid, DateTime utcTime, Guid agentGuid, Guid instanceGuid) {
		return new InstanceEventVisitor(this, utcTime, eventGuid, agentGuid, instanceGuid);
	}
	
	private sealed class InstanceEventVisitor : IInstanceEventVisitor {
		private readonly EventLogManager eventLogManager;
		private readonly Guid eventGuid;
		private readonly DateTime utcTime;
		private readonly Guid agentGuid;
		private readonly Guid instanceGuid;
		
		public InstanceEventVisitor(EventLogManager eventLogManager, DateTime utcTime, Guid eventGuid, Guid agentGuid, Guid instanceGuid) {
			this.eventLogManager = eventLogManager;
			this.eventGuid = eventGuid;
			this.utcTime = utcTime;
			this.agentGuid = agentGuid;
			this.instanceGuid = instanceGuid;
		}
		
		public void OnLaunchSucceeded(InstanceLaunchSucceededEvent e) {
			eventLogManager.EnqueueItem(eventGuid, utcTime, agentGuid, EventLogEventType.InstanceLaunchSucceeded, instanceGuid.ToString());
		}
		
		public void OnLaunchFailed(InstanceLaunchFailedEvent e) {
			eventLogManager.EnqueueItem(eventGuid, utcTime, agentGuid, EventLogEventType.InstanceLaunchFailed, instanceGuid.ToString(), new Dictionary<string, object?> {
				{ "reason", e.Reason.ToString() },
			});
		}
		
		public void OnCrashed(InstanceCrashedEvent e) {
			eventLogManager.EnqueueItem(eventGuid, utcTime, agentGuid, EventLogEventType.InstanceCrashed, instanceGuid.ToString());
		}
		
		public void OnStopped(InstanceStoppedEvent e) {
			eventLogManager.EnqueueItem(eventGuid, utcTime, agentGuid, EventLogEventType.InstanceStopped, instanceGuid.ToString());
		}
		
		public void OnBackupCompleted(InstanceBackupCompletedEvent e) {
			var eventType = e.Kind switch {
				BackupCreationResultKind.Success when e.Warnings != BackupCreationWarnings.None => EventLogEventType.InstanceBackupSucceededWithWarnings,
				BackupCreationResultKind.Success                                                => EventLogEventType.InstanceBackupSucceeded,
				_                                                                               => EventLogEventType.InstanceBackupFailed,
			};
			
			var dictionary = new Dictionary<string, object?>();
			
			if (eventType == EventLogEventType.InstanceBackupFailed) {
				dictionary["reason"] = e.Kind.ToString();
			}
			
			if (e.Warnings != BackupCreationWarnings.None) {
				dictionary["warnings"] = e.Warnings.ListFlags().Select(static warning => warning.ToString()).ToArray();
			}
			
			eventLogManager.EnqueueItem(eventGuid, utcTime, agentGuid, eventType, instanceGuid.ToString(), dictionary.Count == 0 ? null : dictionary);
		}
	}
}
