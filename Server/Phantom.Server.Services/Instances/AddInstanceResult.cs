namespace Phantom.Server.Services.Instances;

public enum AddInstanceResult {
	Success,
	InstanceAlreadyExists,
	InstanceNameMustNotBeEmpty,
	InstanceMemoryMustNotBeZero,
	AgentNotFound,
	AgentShuttingDown,
	AgentInstanceLimitExceeded,
	AgentMemoryLimitExceeded,
	AgentCommunicationError,
	UnknownError
}

public static class AddInstanceResultExtensions {
	public static string ToSentence(this AddInstanceResult reason) {
		return reason switch {
			AddInstanceResult.Success                     => "Success.",
			AddInstanceResult.InstanceNameMustNotBeEmpty  => "Instance name must not be empty.",
			AddInstanceResult.InstanceMemoryMustNotBeZero => "Memory must not be 0 MB.",
			AddInstanceResult.AgentNotFound               => "Agent not found.",
			AddInstanceResult.AgentShuttingDown           => "Agent is shutting down.",
			AddInstanceResult.AgentInstanceLimitExceeded  => "Agent instance limit exceeded.",
			AddInstanceResult.AgentMemoryLimitExceeded    => "Agent memory limit exceeded.",
			AddInstanceResult.AgentCommunicationError     => "Agent did not reply in time.",
			_                                             => "Unknown error."
		};
	}
}
