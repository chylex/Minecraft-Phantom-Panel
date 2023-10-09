using System.Collections.Immutable;

namespace Phantom.Controller.Minecraft;

public static class JvmArgumentsHelper {
	public static ImmutableArray<string> Split(string arguments) {
		return arguments.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
	}

	public static string Join(ImmutableArray<string> arguments) {
		return string.Join('\n', arguments);
	}

	public static ValidationError? Validate(string arguments) {
		return Validate(Split(arguments));
	}

	private static ValidationError? Validate(ImmutableArray<string> arguments) {
		if (!arguments.All(static argument => argument.StartsWith('-'))) {
			return ValidationError.InvalidFormat;
		}

		// TODO not perfect, but good enough
		if (arguments.Any(static argument => argument.Contains("-Xmx"))) {
			return ValidationError.XmxNotAllowed;
		}
		
		if (arguments.Any(static argument => argument.Contains("-Xms"))) {
			return ValidationError.XmsNotAllowed;
		}
		
		return null;
	}

	public enum ValidationError {
		InvalidFormat,
		XmxNotAllowed,
		XmsNotAllowed
	}

	public static string ToSentence(this ValidationError? result) {
		return result switch {
			ValidationError.InvalidFormat => "Invalid format.",
			ValidationError.XmxNotAllowed => "The -Xmx argument must not be specified manually.",
			ValidationError.XmsNotAllowed => "The -Xms argument must not be specified manually.",
			_                             => throw new ArgumentOutOfRangeException(nameof(result), result, null)
		};
	}
}
