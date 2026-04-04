namespace Phantom.Common.Data.Replies;

public enum ConfigureInstanceResult : byte {
	Success                         = 0,
	CouldNotCreateInstanceDirectory = 1,
}

public static class ConfigureInstanceResultExtensions {
	public static string ToSentence(this ConfigureInstanceResult reason) {
		return reason switch {
			ConfigureInstanceResult.Success                         => "Success.",
			ConfigureInstanceResult.CouldNotCreateInstanceDirectory => "Could not create instance directory.",
			_                                                       => "Unknown error.",
		};
	}
}
