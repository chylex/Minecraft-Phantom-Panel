using System.Security.Claims;
using Phantom.Server.Database;
using Phantom.Server.Services.Users;
using Phantom.Server.Web.Identity.Data;

namespace Phantom.Server.Web.Identity.Authorization;

public sealed class PermissionManager {
	private readonly ApplicationDbContext db;
	private readonly IdentityLookup identityLookup;
	private readonly Dictionary<string, IdentityPermissions> userIdsToPermissionIds = new ();

	public PermissionManager(ApplicationDbContext db, IdentityLookup identityLookup) {
		this.db = db;
		this.identityLookup = identityLookup;
	}

	private IdentityPermissions FetchPermissions(string userId) {
		var userPermissions = db.UserPermissions.Where(up => up.UserId == userId).Select(static up => up.PermissionId);
		var rolePermissions = db.UserRoles.Where(ur => ur.UserId == userId).Join(db.RolePermissions, static ur => ur.RoleId, static rp => rp.RoleId, static (ur, rp) => rp.PermissionId);
		return new IdentityPermissions(userPermissions.Union(rolePermissions));
	}

	private IdentityPermissions GetPermissionsForUserId(string? userId) {
		if (userId == null) {
			return IdentityPermissions.None;
		}

		if (userIdsToPermissionIds.TryGetValue(userId, out var userPermissions)) {
			return userPermissions;
		}
		else {
			return userIdsToPermissionIds[userId] = FetchPermissions(userId);
		}
	}

	public IdentityPermissions GetPermissions(ClaimsPrincipal user) {
		return GetPermissionsForUserId(identityLookup.GetAuthenticatedUserId(user));
	}

	public bool CheckPermission(ClaimsPrincipal user, Permission permission) {
		return GetPermissions(user).Check(permission);
	}
}
