using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Data.Web.Users.AddUserErrors;
using Phantom.Common.Data.Web.Users.PasswordRequirementViolations;
using Phantom.Common.Data.Web.Users.UsernameRequirementViolations;
using Phantom.Controller.Database.Entities;
using Phantom.Utils.Collections;
using Phantom.Utils.Tasks;

namespace Phantom.Controller.Database.Repositories;

public sealed class UserRepository {
	private const int MaxUserNameLength = 40;
	private const int MinimumPasswordLength = 16;

	private static UsernameRequirementViolation? CheckUsernameRequirements(string username) {
		if (string.IsNullOrWhiteSpace(username)) {
			return new IsEmpty();
		}
		else if (username.Length > MaxUserNameLength) {
			return new TooLong(MaxUserNameLength);
		}
		else {
			return null;
		}
	}

	private static ImmutableArray<PasswordRequirementViolation> CheckPasswordRequirements(string password) {
		var violations = ImmutableArray.CreateBuilder<PasswordRequirementViolation>();
		
		if (password.Length < MinimumPasswordLength) {
			violations.Add(new TooShort(MinimumPasswordLength));
		}

		if (!password.Any(char.IsLower)) {
			violations.Add(new MustContainLowercaseLetter());
		}

		if (!password.Any(char.IsUpper)) {
			violations.Add(new MustContainUppercaseLetter());
		}

		if (!password.Any(char.IsDigit)) {
			violations.Add(new MustContainDigit());
		}
		
		return violations.ToImmutable();
	}
	
	private readonly ILazyDbContext db;

	public UserRepository(ILazyDbContext db) {
		this.db = db;
	}

	public Task<ImmutableArray<UserEntity>> GetAll() {
		return db.Ctx.Users.AsAsyncEnumerable().ToImmutableArrayAsync();
	}

	public Task<Dictionary<Guid, T>> GetAllByGuid<T>(Func<UserEntity, T> valueSelector, CancellationToken cancellationToken = default) {
		return db.Ctx.Users.ToDictionaryAsync(static user => user.UserGuid, valueSelector, cancellationToken);
	}

	public async Task<UserEntity?> GetByGuid(Guid guid) {
		return await db.Ctx.Users.FindAsync(guid);
	}

	public Task<UserEntity?> GetByName(string username) {
		return db.Ctx.Users.FirstOrDefaultAsync(user => user.Name == username);
	}
	
	public async Task<Result<UserEntity, AddUserError>> CreateUser(string username, string password) {
		var usernameRequirementViolation = CheckUsernameRequirements(username);
		if (usernameRequirementViolation != null) {
			return new NameIsInvalid(usernameRequirementViolation);
		}

		var passwordRequirementViolations = CheckPasswordRequirements(password);
		if (!passwordRequirementViolations.IsEmpty) {
			return new PasswordIsInvalid(passwordRequirementViolations);
		}

		if (await db.Ctx.Users.AnyAsync(user => user.Name == username)) {
			return new NameAlreadyExists();
		}

		var user = new UserEntity(Guid.NewGuid(), username, UserPasswords.Hash(password));
		
		db.Ctx.Users.Add(user);

		return user;
	}

	public Result<SetUserPasswordError> SetUserPassword(UserEntity user, string password) {
		var requirementViolations = CheckPasswordRequirements(password);
		if (!requirementViolations.IsEmpty) {
			return new Common.Data.Web.Users.SetUserPasswordErrors.PasswordIsInvalid(requirementViolations);
		}

		user.PasswordHash = UserPasswords.Hash(password);

		return Result.Ok;
	}

	public void DeleteUser(UserEntity user) {
		db.Ctx.Users.Remove(user);
	}
}
