using System.Collections.Immutable;

namespace Phantom.Controller.Services.Users;

public abstract record SetUserPasswordError {
	private SetUserPasswordError() {}

	public sealed record UserNotFound : SetUserPasswordError;

	public sealed record PasswordIsInvalid(ImmutableArray<PasswordRequirementViolation> Violations) : SetUserPasswordError;
	
	public sealed record UnknownError : SetUserPasswordError;
}

public static class SetUserPasswordErrorExtensions {
	public static string ToSentences(this SetUserPasswordError error, string delimiter) {
		return error switch {
			SetUserPasswordError.UserNotFound        => "User not found.",
			SetUserPasswordError.PasswordIsInvalid e => string.Join(delimiter, e.Violations.Select(static v => v.ToSentence())),
			_                                        => "Unknown error."
		};
	}
}
