namespace Phantom.Server.Services.Instances; 

public enum AddInstanceResult {
	Success,
	InstanceNameMustNotBeEmpty,
	InstanceMemoryMustNotBeZero,
	AgentNotFound,
	AgentInstanceLimitExceeded,
	AgentMemoryLimitExceeded,
	AgentCommunicationError,
	UnknownError
}
