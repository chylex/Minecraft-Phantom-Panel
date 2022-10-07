namespace Phantom.Common.Data.Replies;

public enum LaunchInstanceResult {
	LaunchInitiated,
	AgentShuttingDown,
	InstanceDoesNotExist,
	InstanceAlreadyLaunching,
	InstanceAlreadyRunning,
	InstanceIsStopping,
	CommunicationError,
	UnknownError
}

public static class LaunchInstanceResultExtensions {
	public static string ToSentence(this LaunchInstanceResult reason) {
		return reason switch {
			LaunchInstanceResult.LaunchInitiated          => "Launch initiated.",
			LaunchInstanceResult.AgentShuttingDown        => "Agent is shutting down.",
			LaunchInstanceResult.InstanceDoesNotExist     => "Instance does not exist.",
			LaunchInstanceResult.InstanceAlreadyLaunching => "Instance is already launching.",
			LaunchInstanceResult.InstanceAlreadyRunning   => "Instance is already running.",
			LaunchInstanceResult.InstanceIsStopping       => "Instance is stopping.",
			LaunchInstanceResult.CommunicationError       => "Communication error.",
			_                                             => "Unknown error."
		};
	}
}
