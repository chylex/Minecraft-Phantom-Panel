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

public static class StopInstanceResultExtensions {
	public static string ToSentence(this StopInstanceResult reason) {
		return reason switch {
			StopInstanceResult.StopInitiated           => "Stopping initiated.",
			StopInstanceResult.AgentShuttingDown       => "Agent is shutting down.",
			StopInstanceResult.InstanceDoesNotExist    => "Instance does not exist.",
			StopInstanceResult.InstanceAlreadyStopping => "Instance is already stopping.",
			StopInstanceResult.InstanceAlreadyStopped  => "Instance is already stopped.",
			StopInstanceResult.CommunicationError      => "Communication error.",
			_                                          => "Unknown error."
		};
	}
}
