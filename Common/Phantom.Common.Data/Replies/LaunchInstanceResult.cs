namespace Phantom.Common.Data.Replies; 

public enum LaunchInstanceResult {
	Success,
	AgentShuttingDown,
	InstanceDoesNotExist,
	InstanceAlreadyRunning,
	JavaRuntimeNotFound,
	CouldNotDownloadMinecraftServer,
	ServerPortNotAllowed,
	ServerPortAlreadyInUse,
	RconPortNotAllowed,
	RconPortAlreadyInUse,
	CommunicationError,
	UnknownError,
}
