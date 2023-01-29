namespace Phantom.Common.Data.Replies;

public enum LaunchInstanceResult : byte {
	UnknownError,
	LaunchInitiated,
	InstanceAlreadyLaunching,
	InstanceAlreadyRunning,
	InstanceIsStopping,
	InstanceLimitExceeded,
	MemoryLimitExceeded
}

public static class LaunchInstanceResultExtensions {
	public static string ToSentence(this LaunchInstanceResult reason) {
		return reason switch {
			LaunchInstanceResult.LaunchInitiated          => "Launch initiated.",
			LaunchInstanceResult.InstanceAlreadyLaunching => "Instance is already launching.",
			LaunchInstanceResult.InstanceAlreadyRunning   => "Instance is already running.",
			LaunchInstanceResult.InstanceIsStopping       => "Instance is stopping.",
			LaunchInstanceResult.InstanceLimitExceeded    => "Agent does not have any more available instances.",
			LaunchInstanceResult.MemoryLimitExceeded      => "Agent does not have enough available memory.",
			_                                             => "Unknown error."
		};
	}
}
