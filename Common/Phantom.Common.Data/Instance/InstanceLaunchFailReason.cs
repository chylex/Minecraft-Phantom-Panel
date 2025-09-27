namespace Phantom.Common.Data.Instance;

public enum InstanceLaunchFailReason : byte {
	UnknownError                           = 0,
	JavaRuntimeNotFound                    = 5,
	CouldNotDownloadMinecraftServer        = 6,
	CouldNotConfigureMinecraftServer       = 7,
	CouldNotPrepareMinecraftServerLauncher = 8,
	CouldNotStartMinecraftServer           = 9,
}
