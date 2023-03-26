namespace Phantom.Common.Data.Replies;

public enum StopInstanceResult : byte {
	StopInitiated           = 1,
	InstanceAlreadyStopping = 2,
	InstanceAlreadyStopped  = 3
}

public static class StopInstanceResultExtensions {
	public static string ToSentence(this StopInstanceResult reason) {
		return reason switch {
			StopInstanceResult.StopInitiated           => "Stopping initiated.",
			StopInstanceResult.InstanceAlreadyStopping => "Instance is already stopping.",
			StopInstanceResult.InstanceAlreadyStopped  => "Instance is already stopped.",
			_                                          => "Unknown error."
		};
	}
}
