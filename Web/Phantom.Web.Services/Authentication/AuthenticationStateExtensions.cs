using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Web.Services.Authentication;

public static class AuthenticationStateExtensions {
	public static AuthenticatedUser? GetAuthenticatedUser(this AuthenticationState authenticationState) {
		return authenticationState.User.GetAuthenticatedUser();
	}
	
	public static AuthenticatedUser? GetAuthenticatedUser(this ClaimsPrincipal claimsPrincipal) {
		return claimsPrincipal is CustomClaimsPrincipal principal ? principal.User : null;
	}
	
	public static PermissionSet GetPermissions(this AuthenticationState authenticationState) {
		return authenticationState.User.GetPermissions();
	}
	
	public static PermissionSet GetPermissions(this ClaimsPrincipal claimsPrincipal) {
		return claimsPrincipal.GetAuthenticatedUser() is {} user ? user.Info.Permissions : PermissionSet.None;
	}
	
	public static bool CheckPermission(this AuthenticationState authenticationState, Permission permission) {
		return authenticationState.User.CheckPermission(permission);
	}
	
	public static bool CheckPermission(this ClaimsPrincipal claimsPrincipal, Permission permission) {
		return claimsPrincipal.GetPermissions().Check(permission);
	}
}
