namespace Phantom.Common.Data.Instance;

public interface IInstanceEventVisitor {
	void OnLaunchSucceeded(InstanceLaunchSucceededEvent e);
	void OnLaunchFailed(InstanceLaunchFailedEvent e);
	void OnCrashed(InstanceCrashedEvent e);
	void OnStopped(InstanceStoppedEvent e);
	void OnBackupCompleted(InstanceBackupCompletedEvent e);
}
