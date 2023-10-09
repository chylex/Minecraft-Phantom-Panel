using System.Collections.Immutable;
using System.Security.Claims;
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

	private readonly IDatabaseProvider databaseProvider;

	public UserManager(IDatabaseProvider databaseProvider) {
		this.databaseProvider = databaseProvider;
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

	public async Task<ImmutableArray<UserEntity>> GetAll() {
		await using var ctx = databaseProvider.Provide();
		return await ctx.Users.AsAsyncEnumerable().ToImmutableArrayAsync();
	}

	public async Task<Dictionary<Guid, T>> GetAllByGuid<T>(Func<UserEntity, T> valueSelector, CancellationToken cancellationToken = default) {
		await using var ctx = databaseProvider.Provide();
		return await ctx.Users.ToDictionaryAsync(static user => user.UserGuid, valueSelector, cancellationToken);
	}

	public async Task<UserEntity?> GetByName(string username) {
		await using var ctx = databaseProvider.Provide();
		return await ctx.Users.FirstOrDefaultAsync(user => user.Name == username);
	}

	public async Task<UserEntity?> GetAuthenticated(string username, string password) {
		await using var ctx = databaseProvider.Provide();
		var user = await ctx.Users.FirstOrDefaultAsync(user => user.Name == username);
		return user != null && UserPasswords.Verify(user, password) ? user : null;
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

		UserEntity newUser;
		try {
			await using var ctx = databaseProvider.Provide();
			
			if (await ctx.Users.AnyAsync(user => user.Name == username)) {
				return Result.Fail<UserEntity, AddUserError>(new AddUserError.NameAlreadyExists());
			}

			newUser = new UserEntity(Guid.NewGuid(), username);
			UserPasswords.Set(newUser, password);

			ctx.Users.Add(newUser);
			await ctx.SaveChangesAsync();
		} catch (Exception e) {
			Logger.Error(e, "Could not create user \"{Name}\".", username);
			return Result.Fail<UserEntity, AddUserError>(new AddUserError.UnknownError());
		}
		
		Logger.Information("Created user \"{Name}\" (GUID {Guid}).", username, newUser.UserGuid);
		return Result.Ok<UserEntity, AddUserError>(newUser);
	}

	public async Task<Result<SetUserPasswordError>> SetUserPassword(Guid guid, string password) {
		UserEntity foundUser;
		
		await using (var ctx = databaseProvider.Provide()) {
			var user = await ctx.Users.FindAsync(guid);
			if (user == null) {
				return Result.Fail<SetUserPasswordError>(new SetUserPasswordError.UserNotFound());
			}

			foundUser = user;
			try {
				var requirementViolations = UserPasswords.CheckRequirements(password);
				if (!requirementViolations.IsEmpty) {
					return Result.Fail<SetUserPasswordError>(new SetUserPasswordError.PasswordIsInvalid(requirementViolations));
				}

				UserPasswords.Set(user, password);
				await ctx.SaveChangesAsync();
			} catch (Exception e) {
				Logger.Error(e, "Could not change password for user \"{Name}\" (GUID {Guid}).", user.Name, user.UserGuid);
				return Result.Fail<SetUserPasswordError>(new SetUserPasswordError.UnknownError());
			}
		}

		Logger.Information("Changed password for user \"{Name}\" (GUID {Guid}).", foundUser.Name, foundUser.UserGuid);
		return Result.Ok<SetUserPasswordError>();
	}

	public async Task<DeleteUserResult> DeleteByGuid(Guid guid) {
		await using var ctx = databaseProvider.Provide();
		var user = await ctx.Users.FindAsync(guid);
		if (user == null) {
			return DeleteUserResult.NotFound;
		}

		try {
			ctx.Users.Remove(user);
			await ctx.SaveChangesAsync();
			return DeleteUserResult.Deleted;
		} catch (Exception e) {
			Logger.Error(e, "Could not delete user \"{Name}\" (GUID {Guid}).", user.Name, user.UserGuid);
			return DeleteUserResult.Failed;
		}
	}
}
