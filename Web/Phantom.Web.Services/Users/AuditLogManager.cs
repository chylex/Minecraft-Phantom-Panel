using System.Collections.Immutable;
using Phantom.Common.Data.Web.AuditLog;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Web.Services.Rpc;

namespace Phantom.Web.Services.Users; 

public sealed class AuditLogManager {
	private readonly ControllerConnection controllerConnection;
	
	public AuditLogManager(ControllerConnection controllerConnection) {
		this.controllerConnection = controllerConnection;
	}

	public Task<ImmutableArray<AuditLogItem>> GetMostRecentItems(int count, CancellationToken cancellationToken) {
		var message = new GetAuditLogMessage(count);
		return controllerConnection.Send<GetAuditLogMessage, ImmutableArray<AuditLogItem>>(message, cancellationToken);
	}
}
