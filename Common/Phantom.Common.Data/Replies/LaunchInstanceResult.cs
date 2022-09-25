namespace Phantom.Common.Data.Replies; 

public enum LaunchInstanceResult {
	Success,
	AgentShuttingDown,
	InstanceDoesNotExist,
	InstanceAlreadyRunning,
	JavaRuntimeNotFound,
	CouldNotDownloadMinecraftServer,
	ServerPortAlreadyInUse,
	RconPortAlreadyInUse,
	CommunicationError,
	UnknownError,
}
