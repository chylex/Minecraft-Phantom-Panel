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
