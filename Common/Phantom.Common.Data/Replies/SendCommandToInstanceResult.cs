namespace Phantom.Common.Data.Replies;

public enum SendCommandToInstanceResult {
	Success,
	InstanceDoesNotExist,
	AgentShuttingDown,
	AgentCommunicationError,
	UnknownError
}

public static class SendCommandToInstanceResultExtensions {
	public static string ToSentence(this SendCommandToInstanceResult reason) {
		return reason switch {
			SendCommandToInstanceResult.Success                 => "Command sent.",
			SendCommandToInstanceResult.InstanceDoesNotExist    => "Instance does not exist.",
			SendCommandToInstanceResult.AgentShuttingDown       => "Agent is shutting down.",
			SendCommandToInstanceResult.AgentCommunicationError => "Agent did not reply in time.",
			_                                                   => "Unknown error."
		};
	}
}
