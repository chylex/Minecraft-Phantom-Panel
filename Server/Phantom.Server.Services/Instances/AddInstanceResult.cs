namespace Phantom.Server.Services.Instances;

public enum AddInstanceResult : byte {
	UnknownError,
	Success,
	InstanceAlreadyExists,
	InstanceNameMustNotBeEmpty,
	InstanceMemoryMustNotBeZero,
	AgentNotFound
}

public static class AddInstanceResultExtensions {
	public static string ToSentence(this AddInstanceResult reason) {
		return reason switch {
			AddInstanceResult.Success                     => "Success.",
			AddInstanceResult.InstanceNameMustNotBeEmpty  => "Instance name must not be empty.",
			AddInstanceResult.InstanceMemoryMustNotBeZero => "Memory must not be 0 MB.",
			AddInstanceResult.AgentNotFound               => "Agent not found.",
			_                                             => "Unknown error."
		};
	}
}
