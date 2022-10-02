namespace Phantom.Common.Data.Replies; 

public enum StopInstanceResult {
	StopInitiated,
	AgentShuttingDown,
	InstanceDoesNotExist,
	InstanceAlreadyStopping,
	InstanceAlreadyStopped,
	CommunicationError,
	UnknownError
}
