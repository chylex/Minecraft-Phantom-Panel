using Phantom.Server.Database.Entities;
using Phantom.Server.Services.Audit;
using Phantom.Server.Web.Identity.Interfaces;

namespace Phantom.Server.Web.Base;

sealed class LoginEvents : ILoginEvents {
	private readonly AuditLog auditLog;

	public LoginEvents(AuditLog auditLog) {
		this.auditLog = auditLog;
	}

	public void UserLoggedIn(UserEntity user) {
		auditLog.AddUserLoggedInEvent(user);
	}

	public void UserLoggedOut(Guid userGuid) {
		auditLog.AddUserLoggedOutEvent(userGuid);
	}
}
