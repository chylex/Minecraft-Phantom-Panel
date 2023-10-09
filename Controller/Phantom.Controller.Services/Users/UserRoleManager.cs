using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Logging;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Entities;
using Phantom.Utils.Collections;
using ILogger = Serilog.ILogger;

namespace Phantom.Controller.Services.Users;

public sealed class UserRoleManager {
	private static readonly ILogger Logger = PhantomLogger.Create<UserRoleManager>();

	private readonly ApplicationDbContext db;

	public UserRoleManager(ApplicationDbContext db) {
		this.db = db;
	}

	public Task<Dictionary<Guid, ImmutableArray<RoleEntity>>> GetAllByUserGuid() {
		return db.UserRoles
		         .Include(static ur => ur.Role)
		         .GroupBy(static ur => ur.UserGuid, static ur => ur.Role)
		         .ToDictionaryAsync(static group => group.Key, static group => group.ToImmutableArray());
	}

	public Task<ImmutableArray<RoleEntity>> GetUserRoles(UserEntity user) {
		return db.UserRoles
		         .Include(static ur => ur.Role)
		         .Where(ur => ur.UserGuid == user.UserGuid)
		         .Select(static ur => ur.Role)
		         .AsAsyncEnumerable()
		         .ToImmutableArrayAsync();
	}

	public Task<ImmutableHashSet<Guid>> GetUserRoleGuids(UserEntity user) {
		return db.UserRoles
		         .Where(ur => ur.UserGuid == user.UserGuid)
		         .Select(static ur => ur.RoleGuid)
		         .AsAsyncEnumerable()
		         .ToImmutableSetAsync();
	}

	public async Task<bool> Add(UserEntity user, RoleEntity role) {
		try {
			var userRole = await db.UserRoles.FindAsync(user.UserGuid, role.RoleGuid);
			if (userRole == null) {
				userRole = new UserRoleEntity(user.UserGuid, role.RoleGuid);
				db.UserRoles.Add(userRole);
				await db.SaveChangesAsync();
			}

			Logger.Information("Added user \"{UserName}\" (GUID {UserGuid}) to role \"{RoleName}\" (GUID {RoleGuid}).", user.Name, user.UserGuid, role.Name, role.RoleGuid);
			return true;
		} catch (Exception e) {
			Logger.Error(e, "Could not add user \"{UserName}\" (GUID {UserGuid}) to role \"{RoleName}\" (GUID {RoleGuid}).", user.Name, user.UserGuid, role.Name, role.RoleGuid);
			return false;
		}
	}

	public async Task<bool> Remove(UserEntity user, RoleEntity role) {
		try {
			var userRole = await db.UserRoles.FindAsync(user.UserGuid, role.RoleGuid);
			if (userRole != null) {
				db.UserRoles.Remove(userRole);
				await db.SaveChangesAsync();
			}

			Logger.Information("Removed user \"{UserName}\" (GUID {UserGuid}) from role \"{RoleName}\" (GUID {RoleGuid}).", user.Name, user.UserGuid, role.Name, role.RoleGuid);
			return true;
		} catch (Exception e) {
			Logger.Error(e, "Could not remove user \"{UserName}\" (GUID {UserGuid}) from role \"{RoleName}\" (GUID {RoleGuid}).", user.Name, user.UserGuid, role.Name, role.RoleGuid);
			return false;
		}
	}
}
