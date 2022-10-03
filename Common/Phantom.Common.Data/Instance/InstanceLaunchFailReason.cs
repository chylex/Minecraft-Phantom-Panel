namespace Phantom.Common.Data.Instance;

public enum InstanceLaunchFailReason {
	ServerPortNotAllowed,
	ServerPortAlreadyInUse,
	RconPortNotAllowed,
	RconPortAlreadyInUse,
	JavaRuntimeNotFound,
	CouldNotDownloadMinecraftServer,
	UnknownError
}

public static class InstanceLaunchFailReasonExtensions {
	public static string ToSentence(this InstanceLaunchFailReason reason) {
		return reason switch {
			InstanceLaunchFailReason.ServerPortNotAllowed            => "Server port not allowed.",
			InstanceLaunchFailReason.ServerPortAlreadyInUse          => "Server port already in use.",
			InstanceLaunchFailReason.RconPortNotAllowed              => "Rcon port not allowed.",
			InstanceLaunchFailReason.RconPortAlreadyInUse            => "Rcon port already in use.",
			InstanceLaunchFailReason.JavaRuntimeNotFound             => "Java runtime not found.",
			InstanceLaunchFailReason.CouldNotDownloadMinecraftServer => "Could not download Minecraft server.",
			_                                                        => "Unknown error."
		};
	}
}
