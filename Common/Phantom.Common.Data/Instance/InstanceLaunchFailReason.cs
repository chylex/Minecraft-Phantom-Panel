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
