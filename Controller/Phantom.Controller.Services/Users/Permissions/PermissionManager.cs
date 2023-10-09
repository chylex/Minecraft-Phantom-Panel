using System.Collections.Immutable;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Logging;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Entities;
using Phantom.Utils.Collections;
using ILogger = Serilog.ILogger;

namespace Phantom.Controller.Services.Users.Permissions;

public sealed class PermissionManager {
	private static readonly ILogger Logger = PhantomLogger.Create<PermissionManager>();
	
	private readonly IDatabaseProvider databaseProvider;
	private readonly Dictionary<Guid, IdentityPermissions> userIdsToPermissionIds = new ();

	public PermissionManager(IDatabaseProvider databaseProvider) {
		this.databaseProvider = databaseProvider;
	}

	internal async Task Initialize() {
		Logger.Information("Adding default permissions to database.");
		
		await using var ctx = databaseProvider.Provide();
		
		var existingPermissionIds = await ctx.Permissions.Select(static p => p.Id).AsAsyncEnumerable().ToImmutableSetAsync();
		var missingPermissionIds = GetMissingPermissionsOrdered(Permission.All, existingPermissionIds);
		if (!missingPermissionIds.IsEmpty) {
			Logger.Information("Adding default permissions: {Permissions}", string.Join(", ", missingPermissionIds));
			
			foreach (var permissionId in missingPermissionIds) {
				ctx.Permissions.Add(new PermissionEntity(permissionId));
			}
			
			await ctx.SaveChangesAsync();
		}
	}
	
	internal static ImmutableArray<string> GetMissingPermissionsOrdered(IEnumerable<Permission> allPermissions, ImmutableHashSet<string> existingPermissionIds) {
		return allPermissions.Select(static permission => permission.Id).Except(existingPermissionIds).Order().ToImmutableArray();
	}

	private IdentityPermissions FetchPermissionsForUserId(Guid userId) {
		using var ctx = databaseProvider.Provide();
		var userPermissions = ctx.UserPermissions.Where(up => up.UserGuid == userId).Select(static up => up.PermissionId);
		var rolePermissions = ctx.UserRoles.Where(ur => ur.UserGuid == userId).Join(ctx.RolePermissions, static ur => ur.RoleGuid, static rp => rp.RoleGuid, static (ur, rp) => rp.PermissionId);
		return new IdentityPermissions(userPermissions.Union(rolePermissions));
	}

	private IdentityPermissions GetPermissionsForUserId(Guid userId, bool refreshCache) {
		if (!refreshCache && userIdsToPermissionIds.TryGetValue(userId, out var userPermissions)) {
			return userPermissions;
		}
		else {
			return userIdsToPermissionIds[userId] = FetchPermissionsForUserId(userId);
		}
	}

	public IdentityPermissions GetPermissions(ClaimsPrincipal user, bool refreshCache = false) {
		Guid? userId = UserManager.GetAuthenticatedUserId(user);
		return userId == null ? IdentityPermissions.None : GetPermissionsForUserId(userId.Value, refreshCache);
	}

	public bool CheckPermission(ClaimsPrincipal user, Permission permission, bool refreshCache = false) {
		return GetPermissions(user, refreshCache).Check(permission);
	}
}
