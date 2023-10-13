using System.Collections.Immutable;
using Phantom.Common.Data.Web.EventLog;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Web.Services.Rpc;

namespace Phantom.Web.Services.Events; 

public sealed class EventLogManager {
	private readonly ControllerConnection controllerConnection;
	
	public EventLogManager(ControllerConnection controllerConnection) {
		this.controllerConnection = controllerConnection;
	}

	public Task<ImmutableArray<EventLogItem>> GetMostRecentItems(int count, CancellationToken cancellationToken) {
		var message = new GetEventLogMessage(count);
		return controllerConnection.Send<GetEventLogMessage, ImmutableArray<EventLogItem>>(message, cancellationToken);
	}
}
