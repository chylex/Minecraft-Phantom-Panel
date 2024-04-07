using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.EventLog;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Web.Services.Authentication;
using Phantom.Web.Services.Rpc;

namespace Phantom.Web.Services.Events; 

public sealed class EventLogManager {
	private readonly ControllerConnection controllerConnection;
	
	public EventLogManager(ControllerConnection controllerConnection) {
		this.controllerConnection = controllerConnection;
	}

	public async Task<Result<ImmutableArray<EventLogItem>, UserActionFailure>> GetMostRecentItems(AuthenticatedUser? authenticatedUser, int count, CancellationToken cancellationToken) {
		if (authenticatedUser != null && authenticatedUser.Info.CheckPermission(Permission.ViewEvents)) {
			var message = new GetEventLogMessage(authenticatedUser.Token, count);
			return await controllerConnection.Send<GetEventLogMessage, Result<ImmutableArray<EventLogItem>, UserActionFailure>>(message, cancellationToken);
		}
		else {
			return UserActionFailure.NotAuthorized;
		}
	}
}
