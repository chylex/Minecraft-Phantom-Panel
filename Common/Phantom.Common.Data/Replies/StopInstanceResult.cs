namespace Phantom.Common.Data.Replies;

public enum StopInstanceResult : byte {
	StopInitiated           = 1,
	InstanceAlreadyStopping = 2,
	InstanceAlreadyStopped  = 3
}
