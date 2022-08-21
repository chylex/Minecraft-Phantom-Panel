namespace Phantom.Common.Data; 

public enum CreateInstanceResult {
	Success,
	InstanceAlreadyExists,
	InstanceLimitExceeded,
	MemoryLimitExceeded,
	ServerPortInUse,
	RconPortInUse,
	AgentShuttingDown
}
