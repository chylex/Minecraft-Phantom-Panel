using Phantom.Server.Services.Audit;
using Phantom.Server.Web.Identity.Interfaces;

namespace Phantom.Server.Web.Base;

sealed class LoginEvents : ILoginEvents {
	private readonly AuditLog auditLog;

	public LoginEvents(AuditLog auditLog) {
		this.auditLog = auditLog;
	}

	public void UserLoggedIn(string userId) {
		auditLog.AddUserLoggedInEvent(userId);
	}

	public void UserLoggedOut(string userId) {
		auditLog.AddUserLoggedOutEvent(userId);
	}
}
