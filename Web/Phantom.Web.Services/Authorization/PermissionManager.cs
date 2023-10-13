using System.Security.Claims;
using Phantom.Common.Data.Web.Users;
using Phantom.Web.Services.Authentication;
using UserInfo = Phantom.Web.Services.Authentication.UserInfo;

namespace Phantom.Web.Services.Authorization;

public sealed class PermissionManager {
	private readonly UserSessionManager sessionManager;
	
	public PermissionManager(UserSessionManager sessionManager) {
		this.sessionManager = sessionManager;
	}

	public PermissionSet GetPermissions(ClaimsPrincipal user) {
		return UserInfo.TryGetGuid(user) is {} guid && sessionManager.Find(guid) is {} info ? info.Permissions : PermissionSet.None;
	}
	
	public bool CheckPermission(ClaimsPrincipal user, Permission permission) {
		return GetPermissions(user).Check(permission);
	}
}
