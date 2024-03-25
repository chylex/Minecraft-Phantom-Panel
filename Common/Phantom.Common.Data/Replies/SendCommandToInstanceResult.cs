namespace Phantom.Common.Data.Replies;

public enum SendCommandToInstanceResult : byte {
	UnknownError = 0,
	Success = 1,
	InstanceNotRunning = 2
}
