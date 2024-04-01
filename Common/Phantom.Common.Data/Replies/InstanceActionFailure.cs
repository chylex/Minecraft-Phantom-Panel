namespace Phantom.Common.Data.Replies;

public enum InstanceActionFailure : byte {
	AgentDoesNotExist,
	AgentShuttingDown,
	AgentIsNotResponding,
	InstanceDoesNotExist
}

public static class InstanceActionFailureExtensions {
	public static string ToSentence(this InstanceActionFailure failure) {
		return failure switch {
			InstanceActionFailure.AgentDoesNotExist    => "Agent does not exist.",
			InstanceActionFailure.AgentShuttingDown    => "Agent is shutting down.",
			InstanceActionFailure.AgentIsNotResponding => "Agent is not responding.",
			InstanceActionFailure.InstanceDoesNotExist => "Instance does not exist.",
			_                                          => "Unknown error."
		};
	}
}
