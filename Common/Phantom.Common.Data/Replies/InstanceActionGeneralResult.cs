namespace Phantom.Common.Data.Replies;

public enum InstanceActionGeneralResult : byte {
	None,
	AgentDoesNotExist,
	AgentShuttingDown,
	AgentIsNotResponding,
	InstanceDoesNotExist
}
