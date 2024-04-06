using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.Users;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Repositories;
using Phantom.Controller.Services.Users.Sessions;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Controller.Services.Users; 

sealed class UserRoleManager {
	private static readonly ILogger Logger = PhantomLogger.Create<UserRoleManager>();

	private readonly AuthenticatedUserCache authenticatedUserCache;
	private readonly IDbContextProvider dbProvider;
	
	public UserRoleManager(AuthenticatedUserCache authenticatedUserCache, IDbContextProvider dbProvider) {
		this.authenticatedUserCache = authenticatedUserCache;
		this.dbProvider = dbProvider;
	}

	public async Task<ImmutableDictionary<Guid, ImmutableArray<Guid>>> GetUserRoles(ImmutableHashSet<Guid> userGuids) {
		await using var db = dbProvider.Lazy();
		return await new UserRoleRepository(db).GetRoleGuidsByUserGuid(userGuids);
	}

	public async Task<Result<ChangeUserRolesResult, UserActionFailure>> ChangeUserRoles(LoggedInUser loggedInUser, Guid subjectUserGuid, ImmutableHashSet<Guid> addToRoleGuids, ImmutableHashSet<Guid> removeFromRoleGuids) {
		if (!loggedInUser.CheckPermission(Permission.EditUsers)) {
			return UserActionFailure.NotAuthorized;
		}
		
		await using var db = dbProvider.Lazy();
		var userRepository = new UserRepository(db);
		
		var user = await userRepository.GetByGuid(subjectUserGuid);
		if (user == null) {
			return new ChangeUserRolesResult(ImmutableHashSet<Guid>.Empty, ImmutableHashSet<Guid>.Empty);
		}

		var roleRepository = new RoleRepository(db);
		var userRoleRepository = new UserRoleRepository(db);
		var auditLogWriter = new AuditLogRepository(db).Writer(loggedInUser.Guid);
		
		var rolesByGuid = await roleRepository.GetByGuids(addToRoleGuids.Union(removeFromRoleGuids));
		
		var addedToRoleGuids = ImmutableHashSet.CreateBuilder<Guid>();
		var addedToRoleNames = new List<string>();
		
		var removedFromRoleGuids = ImmutableHashSet.CreateBuilder<Guid>();
		var removedFromRoleNames = new List<string>();
        
		try {
			foreach (var roleGuid in addToRoleGuids) {
				if (rolesByGuid.TryGetValue(roleGuid, out var role)) {
					await userRoleRepository.Add(user, role);
					addedToRoleGuids.Add(roleGuid);
					addedToRoleNames.Add(role.Name);
				}
			}
			
			foreach (var roleGuid in removeFromRoleGuids) {
				if (rolesByGuid.TryGetValue(roleGuid, out var role)) {
					await userRoleRepository.Remove(user, role);
					removedFromRoleGuids.Add(roleGuid);
					removedFromRoleNames.Add(role.Name);
				}
			}

			auditLogWriter.UserRolesChanged(user, addedToRoleNames, removedFromRoleNames);
			await db.Ctx.SaveChangesAsync();
			
			await authenticatedUserCache.Update(user, db);
			
			Logger.Information("Changed roles for user \"{Username}\" (GUID {Guid}).", user.Name, user.UserGuid);
			return new ChangeUserRolesResult(addedToRoleGuids.ToImmutable(), removedFromRoleGuids.ToImmutable());
		} catch (Exception e) {
			Logger.Error(e, "Could not change roles for user \"{Username}\" (GUID {Guid}).", user.Name, user.UserGuid);
			return new ChangeUserRolesResult(ImmutableHashSet<Guid>.Empty, ImmutableHashSet<Guid>.Empty);
		}
	}
}
