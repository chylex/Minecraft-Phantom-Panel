namespace Phantom.Common.Data.Replies; 

public enum ConfigureInstanceResult {
	Success,
	UnknownJavaRuntime,
	InstanceLimitExceeded,
	MemoryLimitExceeded,
	AgentShuttingDown
}
