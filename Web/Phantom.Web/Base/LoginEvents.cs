using Phantom.Controller.Database.Entities;
using Phantom.Controller.Services.Audit;
using Phantom.Web.Identity.Interfaces;

namespace Phantom.Web.Base;

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
