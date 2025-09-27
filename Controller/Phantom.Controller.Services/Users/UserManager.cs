using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Data.Web.Users.CreateOrUpdateAdministratorUserResults;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Entities;
using Phantom.Controller.Database.Repositories;
using Phantom.Controller.Services.Users.Sessions;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Controller.Services.Users;

sealed class UserManager {
	private static readonly ILogger Logger = PhantomLogger.Create<UserManager>();
	
	private readonly AuthenticatedUserCache authenticatedUserCache;
	private readonly ControllerState controllerState;
	private readonly IDbContextProvider dbProvider;
	
	public UserManager(AuthenticatedUserCache authenticatedUserCache, ControllerState controllerState, IDbContextProvider dbProvider) {
		this.authenticatedUserCache = authenticatedUserCache;
		this.controllerState = controllerState;
		this.dbProvider = dbProvider;
	}
	
	public async Task<ImmutableArray<UserInfo>> GetAll() {
		await using var db = dbProvider.Lazy();
		var userRepository = new UserRepository(db);
		
		var allUsers = await userRepository.GetAll();
		return [..allUsers.Select(static user => user.ToUserInfo())];
	}
	
	public async Task<UserEntity?> GetAuthenticated(string username, string password) {
		await using var db = dbProvider.Lazy();
		var userRepository = new UserRepository(db);
		
		var user = await userRepository.GetByName(username);
		return user != null && UserPasswords.Verify(password, user.PasswordHash) ? user : null;
	}
	
	public async Task<CreateOrUpdateAdministratorUserResult> CreateOrUpdateAdministrator(string username, string password) {
		await using var db = dbProvider.Lazy();
		var userRepository = new UserRepository(db);
		var auditLogWriter = new AuditLogRepository(db).Writer(currentUserGuid: null);
		
		try {
			bool wasCreated;
			
			var user = await userRepository.GetByName(username);
			if (user == null) {
				var result = await userRepository.CreateUser(username, password);
				if (result) {
					user = result.Value;
					auditLogWriter.AdministratorUserCreated(user);
					wasCreated = true;
				}
				else {
					return new CreationFailed(result.Error);
				}
			}
			else {
				if (userRepository.SetUserPassword(user, password).TryGetError(out var error)) {
					return new UpdatingFailed(error);
				}
				
				auditLogWriter.AdministratorUserModified(user);
				wasCreated = false;
			}
			
			var role = await new RoleRepository(db).GetByGuid(Role.Administrator.Guid);
			if (role == null) {
				return new AddingToRoleFailed();
			}
			
			await new UserRoleRepository(db).Add(user, role);
			await db.Ctx.SaveChangesAsync();
			
			// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
			if (wasCreated) {
				Logger.Information("Created administrator user \"{Username}\" (GUID {Guid}).", username, user.UserGuid);
			}
			else {
				Logger.Information("Updated administrator user \"{Username}\" (GUID {Guid}).", username, user.UserGuid);
			}
			
			return new Success(user.ToUserInfo());
		} catch (Exception e) {
			Logger.Error(e, "Could not create or update administrator user \"{Username}\".", username);
			return new UnknownError();
		}
	}
	
	public async Task<Result<CreateUserResult, UserActionFailure>> Create(LoggedInUser loggedInUser, string username, string password) {
		if (!loggedInUser.CheckPermission(Permission.EditUsers)) {
			return UserActionFailure.NotAuthorized;
		}
		
		await using var db = dbProvider.Lazy();
		var userRepository = new UserRepository(db);
		var auditLogWriter = new AuditLogRepository(db).Writer(loggedInUser.Guid);
		
		try {
			var result = await userRepository.CreateUser(username, password);
			if (!result) {
				return new Common.Data.Web.Users.CreateUserResults.CreationFailed(result.Error);
			}
			
			var user = result.Value;
			
			auditLogWriter.UserCreated(user);
			await db.Ctx.SaveChangesAsync();
			
			Logger.Information("Created user \"{Username}\" (GUID {Guid}).", username, user.UserGuid);
			return new Common.Data.Web.Users.CreateUserResults.Success(user.ToUserInfo());
		} catch (Exception e) {
			Logger.Error(e, "Could not create user \"{Username}\".", username);
			return new Common.Data.Web.Users.CreateUserResults.UnknownError();
		}
	}
	
	public async Task<Result<DeleteUserResult, UserActionFailure>> DeleteByGuid(LoggedInUser loggedInUser, Guid userGuid) {
		if (!loggedInUser.CheckPermission(Permission.EditUsers)) {
			return UserActionFailure.NotAuthorized;
		}
		
		await using var db = dbProvider.Lazy();
		var userRepository = new UserRepository(db);
		
		var user = await userRepository.GetByGuid(userGuid);
		if (user == null) {
			return DeleteUserResult.NotFound;
		}
		
		authenticatedUserCache.Remove(userGuid);
		
		var auditLogWriter = new AuditLogRepository(db).Writer(loggedInUser.Guid);
		try {
			userRepository.DeleteUser(user);
			auditLogWriter.UserDeleted(user);
			await db.Ctx.SaveChangesAsync();
			
			// In case the user logged in during deletion.
			authenticatedUserCache.Remove(userGuid);
			controllerState.UpdateOrDeleteUser(userGuid);
			
			Logger.Information("Deleted user \"{Username}\" (GUID {Guid}).", user.Name, user.UserGuid);
			return DeleteUserResult.Deleted;
		} catch (Exception e) {
			Logger.Error(e, "Could not delete user \"{Username}\" (GUID {Guid}).", user.Name, user.UserGuid);
			return DeleteUserResult.Failed;
		}
	}
}
