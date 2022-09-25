namespace Phantom.Server.Services.Instances; 

public enum AddInstanceResult {
	Success,
	InstanceAlreadyExists,
	InstanceNameMustNotBeEmpty,
	InstanceMemoryMustNotBeZero,
	AgentNotFound,
	AgentInstanceLimitExceeded,
	AgentMemoryLimitExceeded,
	AgentCommunicationError,
	UnknownError
}
