using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Web.Services.Authentication;

public static class AuthenticationStateExtensions {
	public static Guid? TryGetGuid(this AuthenticationState authenticationState) {
		return authenticationState.User is CustomClaimsPrincipal customUser ? customUser.UserInfo.Guid : null;
	}

	public static PermissionSet GetPermissions(this ClaimsPrincipal user) {
		return user is CustomClaimsPrincipal customUser ? customUser.UserInfo.Permissions : PermissionSet.None;
	}

	public static bool CheckPermission(this ClaimsPrincipal user, Permission permission) {
		return user.GetPermissions().Check(permission);
	}

	public static PermissionSet GetPermissions(this AuthenticationState authenticationState) {
		return authenticationState.User.GetPermissions();
	}

	public static bool CheckPermission(this AuthenticationState authenticationState, Permission permission) {
		return authenticationState.User.CheckPermission(permission);
	}
}
