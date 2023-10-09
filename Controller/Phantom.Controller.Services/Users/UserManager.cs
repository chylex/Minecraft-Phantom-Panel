using System.Collections.Immutable;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Logging;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Entities;
using Phantom.Utils.Collections;
using Phantom.Utils.Tasks;
using ILogger = Serilog.ILogger;

namespace Phantom.Controller.Services.Users;

public sealed class UserManager {
	private static readonly ILogger Logger = PhantomLogger.Create<UserManager>();

	private const int MaxUserNameLength = 40;

	private readonly ApplicationDbContext db;

	public UserManager(ApplicationDbContext db) {
		this.db = db;
	}

	public static Guid? GetAuthenticatedUserId(ClaimsPrincipal user) {
		if (user.Identity is not { IsAuthenticated: true }) {
			return null;
		}

		var claim = user.FindFirst(ClaimTypes.NameIdentifier);
		if (claim == null) {
			return null;
		}

		return Guid.TryParse(claim.Value, out var guid) ? guid : null;
	}

	public Task<ImmutableArray<UserEntity>> GetAll() {
		return db.Users.AsAsyncEnumerable().ToImmutableArrayAsync();
	}

	public Task<Dictionary<Guid, T>> GetAllByGuid<T>(Func<UserEntity, T> valueSelector, CancellationToken cancellationToken = default) {
		return db.Users.ToDictionaryAsync(static user => user.UserGuid, valueSelector, cancellationToken);
	}

	public Task<UserEntity?> GetByName(string username) {
		return db.Users.FirstOrDefaultAsync(user => user.Name == username);
	}

	public async Task<UserEntity?> GetAuthenticated(string username, string password) {
		var user = await db.Users.FirstOrDefaultAsync(user => user.Name == username);
		if (user == null) {
			return null;
		}

		switch (UserPasswords.Verify(user, password)) {
			case PasswordVerificationResult.SuccessRehashNeeded:
				try {
					UserPasswords.Set(user, password);
					await db.SaveChangesAsync();
				} catch (Exception e) {
					Logger.Warning(e, "Could not rehash password for \"{Username}\".", user.Name);
				}

				goto case PasswordVerificationResult.Success;

			case PasswordVerificationResult.Success:
				return user;

			case PasswordVerificationResult.Failed:
				return null;
		}

		throw new InvalidOperationException();
	}

	public async Task<Result<UserEntity, AddUserError>> CreateUser(string username, string password) {
		if (string.IsNullOrWhiteSpace(username)) {
			return Result.Fail<UserEntity, AddUserError>(new AddUserError.NameIsEmpty());
		}
		else if (username.Length > MaxUserNameLength) {
			return Result.Fail<UserEntity, AddUserError>(new AddUserError.NameIsTooLong(MaxUserNameLength));
		}

		var requirementViolations = UserPasswords.CheckRequirements(password);
		if (!requirementViolations.IsEmpty) {
			return Result.Fail<UserEntity, AddUserError>(new AddUserError.PasswordIsInvalid(requirementViolations));
		}

		try {
			if (await db.Users.AnyAsync(user => user.Name == username)) {
				return Result.Fail<UserEntity, AddUserError>(new AddUserError.NameAlreadyExists());
			}

			var guid = Guid.NewGuid();
			var user = new UserEntity(guid, username);
			UserPasswords.Set(user, password);

			db.Users.Add(user);
			await db.SaveChangesAsync();

			Logger.Information("Created user \"{Name}\" (GUID {Guid}).", username, guid);
			return Result.Ok<UserEntity, AddUserError>(user);
		} catch (Exception e) {
			Logger.Error(e, "Could not create user \"{Name}\".", username);
			return Result.Fail<UserEntity, AddUserError>(new AddUserError.UnknownError());
		}
	}

	public async Task<Result<SetUserPasswordError>> SetUserPassword(Guid guid, string password) {
		var user = await db.Users.FindAsync(guid);
		if (user == null) {
			return Result.Fail<SetUserPasswordError>(new SetUserPasswordError.UserNotFound());
		}

		try {
			var requirementViolations = UserPasswords.CheckRequirements(password);
			if (!requirementViolations.IsEmpty) {
				return Result.Fail<SetUserPasswordError>(new SetUserPasswordError.PasswordIsInvalid(requirementViolations));
			}

			UserPasswords.Set(user, password);
			await db.SaveChangesAsync();

			Logger.Information("Changed password for user \"{Name}\" (GUID {Guid}).", user.Name, user.UserGuid);
			return Result.Ok<SetUserPasswordError>();
		} catch (Exception e) {
			Logger.Error(e, "Could not change password for user \"{Name}\" (GUID {Guid}).", user.Name, user.UserGuid);
			return Result.Fail<SetUserPasswordError>(new SetUserPasswordError.UnknownError());
		}
	}

	public async Task<DeleteUserResult> DeleteByGuid(Guid guid) {
		var user = await db.Users.FindAsync(guid);
		if (user == null) {
			return DeleteUserResult.NotFound;
		}

		try {
			db.Users.Remove(user);
			await db.SaveChangesAsync();
			return DeleteUserResult.Deleted;
		} catch (Exception e) {
			Logger.Error(e, "Could not delete user \"{Name}\" (GUID {Guid}).", user.Name, user.UserGuid);
			return DeleteUserResult.Failed;
		}
	}
}
