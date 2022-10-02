namespace Phantom.Common.Data.Replies; 

public enum LaunchInstanceResult {
	LaunchInitiated,
	AgentShuttingDown,
	InstanceDoesNotExist,
	InstanceAlreadyLaunching,
	InstanceAlreadyRunning,
	InstanceIsStopping,
	CommunicationError,
	UnknownError
}
