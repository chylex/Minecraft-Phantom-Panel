using System.Collections.Immutable;
using Phantom.Controller.Database.Entities;

namespace Phantom.Controller.Services.Users;

internal static class UserPasswords {
	private const int MinimumLength = 16;
	
	public static ImmutableArray<PasswordRequirementViolation> CheckRequirements(string password) {
		var violations = ImmutableArray.CreateBuilder<PasswordRequirementViolation>();
		
		if (password.Length < MinimumLength) {
			violations.Add(new PasswordRequirementViolation.TooShort(MinimumLength));
		}

		if (!password.Any(char.IsLower)) {
			violations.Add(new PasswordRequirementViolation.LowercaseLetterRequired());
		}

		if (!password.Any(char.IsUpper)) {
			violations.Add(new PasswordRequirementViolation.UppercaseLetterRequired());
		}

		if (!password.Any(char.IsDigit)) {
			violations.Add(new PasswordRequirementViolation.DigitRequired());
		}
		
		return violations.ToImmutable();
	}

	public static void Set(UserEntity user, string password) {
		user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
	}
	
	public static bool Verify(UserEntity user, string password) {
		return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
	}
}
