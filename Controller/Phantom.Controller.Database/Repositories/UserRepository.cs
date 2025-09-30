using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.Users;
using Phantom.Controller.Database.Entities;
using Phantom.Utils.Collections;

namespace Phantom.Controller.Database.Repositories;

public sealed class UserRepository {
	private const int MaxUserNameLength = 40;
	private const int MinimumPasswordLength = 16;
	
	private static UsernameRequirementViolation? CheckUsernameRequirements(string username) {
		if (string.IsNullOrWhiteSpace(username)) {
			return new UsernameRequirementViolation.IsEmpty();
		}
		else if (username.Length > MaxUserNameLength) {
			return new UsernameRequirementViolation.TooLong(MaxUserNameLength);
		}
		else {
			return null;
		}
	}
	
	private static ImmutableArray<PasswordRequirementViolation> CheckPasswordRequirements(string password) {
		var violations = ImmutableArray.CreateBuilder<PasswordRequirementViolation>();
		
		if (password.Length < MinimumPasswordLength) {
			violations.Add(new PasswordRequirementViolation.TooShort(MinimumPasswordLength));
		}
		
		if (!password.Any(char.IsLower)) {
			violations.Add(new PasswordRequirementViolation.MustContainLowercaseLetter());
		}
		
		if (!password.Any(char.IsUpper)) {
			violations.Add(new PasswordRequirementViolation.MustContainUppercaseLetter());
		}
		
		if (!password.Any(char.IsDigit)) {
			violations.Add(new PasswordRequirementViolation.MustContainDigit());
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
			return new AddUserError.NameIsInvalid(usernameRequirementViolation);
		}
		
		var passwordRequirementViolations = CheckPasswordRequirements(password);
		if (!passwordRequirementViolations.IsEmpty) {
			return new AddUserError.PasswordIsInvalid(passwordRequirementViolations);
		}
		
		if (await db.Ctx.Users.AnyAsync(user => user.Name == username)) {
			return new AddUserError.NameAlreadyExists();
		}
		
		var user = new UserEntity(Guid.NewGuid(), username, UserPasswords.Hash(password));
		
		db.Ctx.Users.Add(user);
		
		return user;
	}
	
	public Result<SetUserPasswordError> SetUserPassword(UserEntity user, string password) {
		var requirementViolations = CheckPasswordRequirements(password);
		if (!requirementViolations.IsEmpty) {
			return new SetUserPasswordError.PasswordIsInvalid(requirementViolations);
		}
		
		user.PasswordHash = UserPasswords.Hash(password);
		
		return Result.Ok;
	}
	
	public void DeleteUser(UserEntity user) {
		db.Ctx.Users.Remove(user);
	}
}
