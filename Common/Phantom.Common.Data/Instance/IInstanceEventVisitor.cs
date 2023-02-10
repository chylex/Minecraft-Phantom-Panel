namespace Phantom.Common.Data.Instance; 

public interface IInstanceEventVisitor {
	void OnLaunchSucceeded(InstanceLaunchSuccededEvent e);
	void OnLaunchFailed(InstanceLaunchFailedEvent e);
	void OnCrashed(InstanceCrashedEvent e);
	void OnStopped(InstanceStoppedEvent e);
	void OnBackupCompleted(InstanceBackupCompletedEvent e);
}
