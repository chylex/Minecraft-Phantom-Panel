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
			LaunchInstanceResult.LaunchInitiated        => "Launch initiated.",
			LaunchInstanceResult.AgentShuttingDown      => "Agent is shutting down.",
			LaunchInstanceResult.InstanceDoesNotExist   => "Instance does not exist.",
			LaunchInstanceResult.InstanceAlreadyRunning => "Instance is already running.",
			// TODO
			// LaunchInstanceResult.JavaRuntimeNotFound => "Java runtime not found.",
			// LaunchInstanceResult.CouldNotDownloadMinecraftServer => "Could not download Minecraft server.",
			// LaunchInstanceResult.ServerPortNotAllowed => "Server port is not allowed.",
			// LaunchInstanceResult.ServerPortAlreadyInUse => "Server port is already in use.",
			// LaunchInstanceResult.RconPortNotAllowed => "Rcon port is not allowed.",
			// LaunchInstanceResult.RconPortAlreadyInUse => "Rcon port is already in use.",
			LaunchInstanceResult.CommunicationError => "Communication error.",
			_                                       => "Unknown error."
		};
	}
}
