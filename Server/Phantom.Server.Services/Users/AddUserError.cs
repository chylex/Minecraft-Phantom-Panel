using System.Collections.Immutable;

namespace Phantom.Server.Services.Users;

public abstract record AddUserError {
	private AddUserError() {}

	public sealed record NameIsEmpty : AddUserError;

	public sealed record NameIsTooLong(int MaximumLength) : AddUserError;

	public sealed record NameAlreadyExists : AddUserError;

	public sealed record PasswordIsInvalid(ImmutableArray<PasswordRequirementViolation> Violations) : AddUserError;

	public sealed record UnknownError : AddUserError;
}

public static class AddUserErrorExtensions {
	public static string ToSentences(this AddUserError error, string delimiter) {
		return error switch {
			AddUserError.NameIsEmpty         => "Name cannot be empty.",
			AddUserError.NameIsTooLong e     => "Name cannot be longer than " + e.MaximumLength + " character(s).",
			AddUserError.NameAlreadyExists   => "Name is already occupied.",
			AddUserError.PasswordIsInvalid e => string.Join(delimiter, e.Violations.Select(static v => v.ToSentence())),
			_                                => "Unknown error."
		};
	}
}
