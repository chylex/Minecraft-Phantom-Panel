using System.Security.Claims;
using Phantom.Server.Database;
using Phantom.Server.Services.Users;
using Phantom.Server.Web.Identity.Data;

namespace Phantom.Server.Web.Identity.Authorization;

public sealed class PermissionManager {
	private readonly DatabaseProvider databaseProvider;
	private readonly Dictionary<Guid, IdentityPermissions> userIdsToPermissionIds = new ();

	public PermissionManager(DatabaseProvider databaseProvider) {
		this.databaseProvider = databaseProvider;
	}

	private IdentityPermissions FetchPermissionsForUserId(Guid userId) {
		using var scope = databaseProvider.CreateScope();
		var userPermissions = scope.Ctx.UserPermissions.Where(up => up.UserGuid == userId).Select(static up => up.PermissionId);
		var rolePermissions = scope.Ctx.UserRoles.Where(ur => ur.UserGuid == userId).Join(scope.Ctx.RolePermissions, static ur => ur.RoleGuid, static rp => rp.RoleGuid, static (ur, rp) => rp.PermissionId);
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
