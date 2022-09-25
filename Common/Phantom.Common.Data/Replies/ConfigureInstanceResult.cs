namespace Phantom.Common.Data.Replies; 

public enum ConfigureInstanceResult {
	Success,
	UnknownJavaRuntime,
	InstanceAlreadyExists,
	InstanceLimitExceeded,
	MemoryLimitExceeded,
	ServerPortInUse,
	RconPortInUse,
	AgentShuttingDown
}
