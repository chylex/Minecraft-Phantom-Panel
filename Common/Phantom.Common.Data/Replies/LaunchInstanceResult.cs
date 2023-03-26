namespace Phantom.Common.Data.Replies;

public enum LaunchInstanceResult : byte {
	LaunchInitiated          = 1,
	InstanceAlreadyLaunching = 2,
	InstanceAlreadyRunning   = 3,
	InstanceLimitExceeded    = 4,
	MemoryLimitExceeded      = 5
}

public static class LaunchInstanceResultExtensions {
	public static string ToSentence(this LaunchInstanceResult reason) {
		return reason switch {
			LaunchInstanceResult.LaunchInitiated          => "Launch initiated.",
			LaunchInstanceResult.InstanceAlreadyLaunching => "Instance is already launching.",
			LaunchInstanceResult.InstanceAlreadyRunning   => "Instance is already running.",
			LaunchInstanceResult.InstanceLimitExceeded    => "Agent does not have any more available instances.",
			LaunchInstanceResult.MemoryLimitExceeded      => "Agent does not have enough available memory.",
			_                                             => "Unknown error."
		};
	}
}
