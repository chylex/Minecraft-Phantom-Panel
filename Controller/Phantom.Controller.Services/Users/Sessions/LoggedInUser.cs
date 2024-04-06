using Phantom.Common.Data.Web.Users;

namespace Phantom.Controller.Services.Users.Sessions;

readonly record struct LoggedInUser(AuthenticatedUserInfo? AuthenticatedUserInfo) {
	public Guid? Guid => AuthenticatedUserInfo?.Guid;
	
	public bool CheckPermission(Permission permission) {
		return AuthenticatedUserInfo != null && AuthenticatedUserInfo.Permissions.Check(permission);
	}
}
