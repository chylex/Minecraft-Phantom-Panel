using System.Collections.Immutable;

namespace Phantom.Controller.Services.Users;

public abstract record SetUserPasswordError {
	private SetUserPasswordError() {}

	public sealed record UserNotFound : SetUserPasswordError;

	public sealed record PasswordIsInvalid(ImmutableArray<PasswordRequirementViolation> Violations) : SetUserPasswordError;
	
	public sealed record UnknownError : SetUserPasswordError;
}
