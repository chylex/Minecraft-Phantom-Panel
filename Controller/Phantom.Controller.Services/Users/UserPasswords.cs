using System.Collections.Immutable;
using Microsoft.AspNetCore.Identity;
using Phantom.Server.Database.Entities;

namespace Phantom.Server.Services.Users;

internal static class UserPasswords {
	private static PasswordHasher<UserEntity> Hasher { get; } = new ();

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
		user.PasswordHash = Hasher.HashPassword(user, password);
	}
	
	public static PasswordVerificationResult Verify(UserEntity user, string password) {
		return Hasher.VerifyHashedPassword(user, user.PasswordHash, password);
	}
}
