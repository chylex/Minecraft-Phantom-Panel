using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.AuditLog;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Web.Services.Authentication;
using Phantom.Web.Services.Rpc;

namespace Phantom.Web.Services.Users; 

public sealed class AuditLogManager {
	private readonly ControllerConnection controllerConnection;
	
	public AuditLogManager(ControllerConnection controllerConnection) {
		this.controllerConnection = controllerConnection;
	}

	public async Task<Result<ImmutableArray<AuditLogItem>, UserActionFailure>> GetMostRecentItems(AuthenticatedUser? authenticatedUser, int count, CancellationToken cancellationToken) {
		if (authenticatedUser != null && authenticatedUser.Info.CheckPermission(Permission.ViewAudit)) {
			var message = new GetAuditLogMessage(authenticatedUser.Token, count);
			return await controllerConnection.Send<GetAuditLogMessage, Result<ImmutableArray<AuditLogItem>, UserActionFailure>>(message, cancellationToken);
		}
		else {
			return UserActionFailure.NotAuthorized;
		}
	}
}
