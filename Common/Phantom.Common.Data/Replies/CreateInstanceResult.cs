namespace Phantom.Common.Data.Replies; 

public enum CreateInstanceResult {
	Success,
	UnknownJavaRuntime,
	InstanceAlreadyExists,
	InstanceLimitExceeded,
	MemoryLimitExceeded,
	ServerPortInUse,
	RconPortInUse,
	AgentShuttingDown
}
