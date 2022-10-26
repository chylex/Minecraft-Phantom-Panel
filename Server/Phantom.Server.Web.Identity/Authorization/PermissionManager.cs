using System.Security.Claims;
using Phantom.Server.Database;
using Phantom.Server.Services.Users;
using Phantom.Server.Web.Identity.Data;

namespace Phantom.Server.Web.Identity.Authorization;

public sealed class PermissionManager {
	private readonly DatabaseProvider databaseProvider;
	private readonly IdentityLookup identityLookup;
	private readonly Dictionary<string, IdentityPermissions> userIdsToPermissionIds = new ();

	public PermissionManager(DatabaseProvider databaseProvider, IdentityLookup identityLookup) {
		this.databaseProvider = databaseProvider;
		this.identityLookup = identityLookup;
	}

	private IdentityPermissions FetchPermissions(string userId) {
		using var scope = databaseProvider.CreateScope();
		var userPermissions = scope.Ctx.UserPermissions.Where(up => up.UserId == userId).Select(static up => up.PermissionId);
		var rolePermissions = scope.Ctx.UserRoles.Where(ur => ur.UserId == userId).Join(scope.Ctx.RolePermissions, static ur => ur.RoleId, static rp => rp.RoleId, static (ur, rp) => rp.PermissionId);
		return new IdentityPermissions(userPermissions.Union(rolePermissions));
	}

	private IdentityPermissions GetPermissionsForUserId(string? userId, bool refreshCache) {
		if (userId == null) {
			return IdentityPermissions.None;
		}

		if (!refreshCache && userIdsToPermissionIds.TryGetValue(userId, out var userPermissions)) {
			return userPermissions;
		}
		else {
			return userIdsToPermissionIds[userId] = FetchPermissions(userId);
		}
	}

	public IdentityPermissions GetPermissions(ClaimsPrincipal user, bool refreshCache = false) {
		return GetPermissionsForUserId(identityLookup.GetAuthenticatedUserId(user), refreshCache);
	}

	public bool CheckPermission(ClaimsPrincipal user, Permission permission, bool refreshCache = false) {
		return GetPermissions(user, refreshCache).Check(permission);
	}
}
