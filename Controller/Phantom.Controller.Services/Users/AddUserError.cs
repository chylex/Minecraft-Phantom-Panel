using System.Collections.Immutable;

namespace Phantom.Controller.Services.Users;

public abstract record AddUserError {
	private AddUserError() {}

	public sealed record NameIsEmpty : AddUserError;

	public sealed record NameIsTooLong(int MaximumLength) : AddUserError;

	public sealed record NameAlreadyExists : AddUserError;

	public sealed record PasswordIsInvalid(ImmutableArray<PasswordRequirementViolation> Violations) : AddUserError;

	public sealed record UnknownError : AddUserError;
}
