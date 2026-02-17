namespace Phantom.Common.Data.Replies;

public enum ConfigureInstanceResult : byte {
	Success                      = 0,
	CouldNotCreateInstanceFolder = 1,
}

public static class ConfigureInstanceResultExtensions {
	public static string ToSentence(this ConfigureInstanceResult reason) {
		return reason switch {
			ConfigureInstanceResult.Success                      => "Success.",
			ConfigureInstanceResult.CouldNotCreateInstanceFolder => "Could not create instance folder.",
			_                                                    => "Unknown error.",
		};
	}
}
