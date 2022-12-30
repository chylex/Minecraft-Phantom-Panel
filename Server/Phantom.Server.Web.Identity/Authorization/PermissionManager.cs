﻿using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Phantom.Server.Database;
using Phantom.Server.Web.Identity.Authentication;
using Phantom.Server.Web.Identity.Data;

namespace Phantom.Server.Web.Identity.Authorization;

public sealed class PermissionManager {
	private readonly DatabaseProvider databaseProvider;
	private readonly UserManager<IdentityUser> userManager;
	private readonly Dictionary<string, IdentityPermissions> userIdsToPermissionIds = new ();

	public PermissionManager(DatabaseProvider databaseProvider, UserManager<IdentityUser> userManager) {
		this.databaseProvider = databaseProvider;
		this.userManager = userManager;
	}

	private IdentityPermissions FetchPermissionsForUserId(string userId) {
		using var scope = databaseProvider.CreateScope();
		var userPermissions = scope.Ctx.UserPermissions.Where(up => up.UserId == userId).Select(static up => up.PermissionId);
		var rolePermissions = scope.Ctx.UserRoles.Where(ur => ur.UserId == userId).Join(scope.Ctx.RolePermissions, static ur => ur.RoleId, static rp => rp.RoleId, static (ur, rp) => rp.PermissionId);
		return new IdentityPermissions(userPermissions.Union(rolePermissions));
	}

	private IdentityPermissions GetPermissionsForUserId(string userId, bool refreshCache) {
		if (!refreshCache && userIdsToPermissionIds.TryGetValue(userId, out var userPermissions)) {
			return userPermissions;
		}
		else {
			return userIdsToPermissionIds[userId] = FetchPermissionsForUserId(userId);
		}
	}

	public IdentityPermissions GetPermissions(ClaimsPrincipal user, bool refreshCache = false) {
		string? userId = PhantomLoginManager.GetAuthenticatedUserId(user, userManager);
		return userId == null ? IdentityPermissions.None : GetPermissionsForUserId(userId, refreshCache);
	}

	public bool CheckPermission(ClaimsPrincipal user, Permission permission, bool refreshCache = false) {
		return GetPermissions(user, refreshCache).Check(permission);
	}
}
