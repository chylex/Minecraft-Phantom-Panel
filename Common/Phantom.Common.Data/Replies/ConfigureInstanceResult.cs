namespace Phantom.Common.Data.Replies;

public enum ConfigureInstanceResult : byte {
	Success
}

public static class ConfigureInstanceResultExtensions {
	public static string ToSentence(this ConfigureInstanceResult reason) {
		return reason switch {
			ConfigureInstanceResult.Success => "Success.",
			_                               => "Unknown error."
		};
	}
}
