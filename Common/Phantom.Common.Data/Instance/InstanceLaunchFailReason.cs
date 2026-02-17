namespace Phantom.Common.Data.Instance;

public enum InstanceLaunchFailReason : byte {
	UnknownError                  = 0,
	CouldNotPrepareServerInstance = 1,
	CouldNotFindServerExecutable  = 2,
	CouldNotStartServerExecutable = 3,
}
