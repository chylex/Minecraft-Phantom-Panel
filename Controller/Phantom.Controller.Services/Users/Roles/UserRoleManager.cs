using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Logging;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Entities;
using Phantom.Utils.Collections;
using ILogger = Serilog.ILogger;

namespace Phantom.Controller.Services.Users.Roles;

public sealed class UserRoleManager {
	private static readonly ILogger Logger = PhantomLogger.Create<UserRoleManager>();

	private readonly IDatabaseProvider databaseProvider;

	public UserRoleManager(IDatabaseProvider databaseProvider) {
		this.databaseProvider = databaseProvider;
	}

	public async Task<Dictionary<Guid, ImmutableArray<RoleEntity>>> GetAllByUserGuid() {
		await using var ctx = databaseProvider.Provide();
		return await ctx.UserRoles
		                .Include(static ur => ur.Role)
		                .GroupBy(static ur => ur.UserGuid, static ur => ur.Role)
		                .ToDictionaryAsync(static group => group.Key, static group => group.ToImmutableArray());
	}

	public async Task<ImmutableArray<RoleEntity>> GetUserRoles(UserEntity user) {
		await using var ctx = databaseProvider.Provide();
		return await ctx.UserRoles
		                .Include(static ur => ur.Role)
		                .Where(ur => ur.UserGuid == user.UserGuid)
		                .Select(static ur => ur.Role)
		                .AsAsyncEnumerable()
		                .ToImmutableArrayAsync();
	}

	public async Task<ImmutableHashSet<Guid>> GetUserRoleGuids(UserEntity user) {
		await using var ctx = databaseProvider.Provide();
		return await ctx.UserRoles
		                .Where(ur => ur.UserGuid == user.UserGuid)
		                .Select(static ur => ur.RoleGuid)
		                .AsAsyncEnumerable()
		                .ToImmutableSetAsync();
	}

	public async Task<bool> Add(UserEntity user, RoleEntity role) {
		try {
			await using var ctx = databaseProvider.Provide();
			
			var userRole = await ctx.UserRoles.FindAsync(user.UserGuid, role.RoleGuid);
			if (userRole == null) {
				userRole = new UserRoleEntity(user.UserGuid, role.RoleGuid);
				ctx.UserRoles.Add(userRole);
				await ctx.SaveChangesAsync();
			}
		} catch (Exception e) {
			Logger.Error(e, "Could not add user \"{UserName}\" (GUID {UserGuid}) to role \"{RoleName}\" (GUID {RoleGuid}).", user.Name, user.UserGuid, role.Name, role.RoleGuid);
			return false;
		}

		Logger.Information("Added user \"{UserName}\" (GUID {UserGuid}) to role \"{RoleName}\" (GUID {RoleGuid}).", user.Name, user.UserGuid, role.Name, role.RoleGuid);
		return true;
	}

	public async Task<bool> Remove(UserEntity user, RoleEntity role) {
		try {
			await using var ctx = databaseProvider.Provide();
			
			var userRole = await ctx.UserRoles.FindAsync(user.UserGuid, role.RoleGuid);
			if (userRole != null) {
				ctx.UserRoles.Remove(userRole);
				await ctx.SaveChangesAsync();
			}
		} catch (Exception e) {
			Logger.Error(e, "Could not remove user \"{UserName}\" (GUID {UserGuid}) from role \"{RoleName}\" (GUID {RoleGuid}).", user.Name, user.UserGuid, role.Name, role.RoleGuid);
			return false;
		}

		Logger.Information("Removed user \"{UserName}\" (GUID {UserGuid}) from role \"{RoleName}\" (GUID {RoleGuid}).", user.Name, user.UserGuid, role.Name, role.RoleGuid);
		return true;
	}
}
