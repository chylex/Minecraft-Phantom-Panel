namespace Phantom.Common.Data.Replies;

public enum LaunchInstanceResult : byte {
	LaunchInitiated            = 1,
	InstanceAlreadyLaunching   = 2,
	InstanceAlreadyRunning     = 3,
	InstanceLimitExceeded      = 4,
	MemoryLimitExceeded        = 5,
	ServerPortNotAllowed       = 6,
	ServerPortAlreadyInUse     = 7,
	AdditionalPortNotAllowed   = 8,
	AdditionalPortAlreadyInUse = 9,
	InvalidConfiguration       = 10,
}
