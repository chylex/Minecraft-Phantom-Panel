namespace Phantom.Common.Data.Instance;

public enum InstanceLaunchFailReason : byte {
	UnknownError                           = 0,
	ServerPortNotAllowed                   = 1,
	ServerPortAlreadyInUse                 = 2,
	RconPortNotAllowed                     = 3,
	RconPortAlreadyInUse                   = 4,
	JavaRuntimeNotFound                    = 5,
	CouldNotDownloadMinecraftServer        = 6,
	CouldNotConfigureMinecraftServer       = 7,
	CouldNotPrepareMinecraftServerLauncher = 8,
	CouldNotStartMinecraftServer           = 9
}

public static class InstanceLaunchFailReasonExtensions {
	public static string ToSentence(this InstanceLaunchFailReason reason) {
		return reason switch {
			InstanceLaunchFailReason.ServerPortNotAllowed                   => "Server port not allowed.",
			InstanceLaunchFailReason.ServerPortAlreadyInUse                 => "Server port already in use.",
			InstanceLaunchFailReason.RconPortNotAllowed                     => "Rcon port not allowed.",
			InstanceLaunchFailReason.RconPortAlreadyInUse                   => "Rcon port already in use.",
			InstanceLaunchFailReason.JavaRuntimeNotFound                    => "Java runtime not found.",
			InstanceLaunchFailReason.CouldNotDownloadMinecraftServer        => "Could not download Minecraft server.",
			InstanceLaunchFailReason.CouldNotConfigureMinecraftServer       => "Could not configure Minecraft server.",
			InstanceLaunchFailReason.CouldNotPrepareMinecraftServerLauncher => "Could not prepare Minecraft server launcher.",
			InstanceLaunchFailReason.CouldNotStartMinecraftServer           => "Could not start Minecraft server.",
			_                                                               => "Unknown error."
		};
	}
}
