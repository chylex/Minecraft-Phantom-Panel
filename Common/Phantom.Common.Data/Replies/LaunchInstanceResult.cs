namespace Phantom.Common.Data.Replies;

public enum LaunchInstanceResult : byte {
	UnknownError,
	LaunchInitiated,
	InstanceAlreadyLaunching,
	InstanceAlreadyRunning,
	InstanceIsStopping
}

public static class LaunchInstanceResultExtensions {
	public static string ToSentence(this LaunchInstanceResult reason) {
		return reason switch {
			LaunchInstanceResult.LaunchInitiated          => "Launch initiated.",
			LaunchInstanceResult.InstanceAlreadyLaunching => "Instance is already launching.",
			LaunchInstanceResult.InstanceAlreadyRunning   => "Instance is already running.",
			LaunchInstanceResult.InstanceIsStopping       => "Instance is stopping.",
			_                                             => "Unknown error."
		};
	}
}
