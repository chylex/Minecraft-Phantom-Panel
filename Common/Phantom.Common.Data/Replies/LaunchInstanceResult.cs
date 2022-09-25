namespace Phantom.Common.Data.Replies; 

public enum LaunchInstanceResult {
	Success,
	AgentShuttingDown,
	InstanceDoesNotExist,
	InstanceAlreadyRunning,
	ServerPortAlreadyInUse,
	RconPortAlreadyInUse,
	CommunicationError,
	UnknownError,
}
