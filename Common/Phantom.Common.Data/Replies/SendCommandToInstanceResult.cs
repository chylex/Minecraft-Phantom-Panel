namespace Phantom.Common.Data.Replies;

public enum SendCommandToInstanceResult : byte {
	UnknownError,
	Success
}

public static class SendCommandToInstanceResultExtensions {
	public static string ToSentence(this SendCommandToInstanceResult reason) {
		return reason switch {
			SendCommandToInstanceResult.Success => "Command sent.",
			_                                   => "Unknown error."
		};
	}
}
