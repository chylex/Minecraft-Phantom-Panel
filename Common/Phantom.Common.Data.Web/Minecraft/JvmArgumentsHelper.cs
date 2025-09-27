using System.Collections.Immutable;

namespace Phantom.Common.Data.Web.Minecraft;

public static class JvmArgumentsHelper {
	public static ImmutableArray<string> Split(string arguments) {
		return [..arguments.Split(separator: '\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
	}
	
	public static string Join(ImmutableArray<string> arguments) {
		return string.Join(separator: '\n', arguments);
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
		XmsNotAllowed,
	}
}
