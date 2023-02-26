namespace Phantom.Common.Data.Instance;

public enum InstanceLaunchFailReason : byte {
	UnknownError,
	ServerPortNotAllowed,
	ServerPortAlreadyInUse,
	RconPortNotAllowed,
	RconPortAlreadyInUse,
	JavaRuntimeNotFound,
	InvalidJvmArguments,
	CouldNotDownloadMinecraftServer,
	CouldNotConfigureMinecraftServer,
	CouldNotPrepareMinecraftServerLauncher,
	CouldNotStartMinecraftServer
}

public static class InstanceLaunchFailReasonExtensions {
	public static string ToSentence(this InstanceLaunchFailReason reason) {
		return reason switch {
			InstanceLaunchFailReason.ServerPortNotAllowed                   => "Server port not allowed.",
			InstanceLaunchFailReason.ServerPortAlreadyInUse                 => "Server port already in use.",
			InstanceLaunchFailReason.RconPortNotAllowed                     => "Rcon port not allowed.",
			InstanceLaunchFailReason.RconPortAlreadyInUse                   => "Rcon port already in use.",
			InstanceLaunchFailReason.JavaRuntimeNotFound                    => "Java runtime not found.",
			InstanceLaunchFailReason.InvalidJvmArguments                    => "Invalid JVM arguments.",
			InstanceLaunchFailReason.CouldNotDownloadMinecraftServer        => "Could not download Minecraft server.",
			InstanceLaunchFailReason.CouldNotConfigureMinecraftServer       => "Could not configure Minecraft server.",
			InstanceLaunchFailReason.CouldNotPrepareMinecraftServerLauncher => "Could not prepare Minecraft server launcher.",
			InstanceLaunchFailReason.CouldNotStartMinecraftServer           => "Could not start Minecraft server.",
			_                                                               => "Unknown error."
		};
	}
}
