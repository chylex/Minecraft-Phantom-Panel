namespace Phantom.Server.Services.Instances; 

public enum AddInstanceResult {
	Success,
	GuidAlreadyExists,
	InstanceNameMustNotBeEmpty,
	InstanceMemoryMustNotBeZero,
	AgentNotFound,
	AgentInstanceLimitExceeded,
	AgentMemoryLimitExceeded,
	AgentCommunicationError
}
