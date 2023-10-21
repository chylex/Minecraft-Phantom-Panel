namespace Phantom.Controller.Services.Users;

public abstract record PasswordRequirementViolation {
	private PasswordRequirementViolation() {}

	public sealed record TooShort(int MinimumLength) : PasswordRequirementViolation;

	public sealed record LowercaseLetterRequired : PasswordRequirementViolation;

	public sealed record UppercaseLetterRequired : PasswordRequirementViolation;

	public sealed record DigitRequired : PasswordRequirementViolation;
}
