namespace Phantom.Common.Data.Replies; 

public enum CreateInstanceResult {
	Success,
	InstanceAlreadyExists,
	InstanceLimitExceeded,
	MemoryLimitExceeded,
	ServerPortInUse,
	RconPortInUse,
	AgentShuttingDown
}
