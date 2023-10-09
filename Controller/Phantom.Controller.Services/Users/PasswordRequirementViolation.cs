namespace Phantom.Controller.Services.Users;

public abstract record PasswordRequirementViolation {
	private PasswordRequirementViolation() {}

	public sealed record TooShort(int MinimumLength) : PasswordRequirementViolation;

	public sealed record LowercaseLetterRequired : PasswordRequirementViolation;

	public sealed record UppercaseLetterRequired : PasswordRequirementViolation;

	public sealed record DigitRequired : PasswordRequirementViolation;
}

public static class PasswordRequirementViolationExtensions {
	public static string ToSentence(this PasswordRequirementViolation violation) {
		return violation switch {
			PasswordRequirementViolation.TooShort v              => "Password must be at least " + v.MinimumLength + " character(s) long.",
			PasswordRequirementViolation.LowercaseLetterRequired => "Password must contain a lowercase letter.",
			PasswordRequirementViolation.UppercaseLetterRequired => "Password must contain an uppercase letter.",
			PasswordRequirementViolation.DigitRequired           => "Password must contain a digit.",
			_                                                    => "Unknown error."
		};
	}
}
