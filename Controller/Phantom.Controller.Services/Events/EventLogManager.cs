using System.Collections.Immutable;
using Akka.Actor;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.EventLog;
using Phantom.Common.Data.Web.Users;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Repositories;
using Phantom.Controller.Services.Users.Sessions;
using Phantom.Utils.Actor;

namespace Phantom.Controller.Services.Events; 

sealed partial class EventLogManager {
	private readonly ControllerState controllerState;
	private readonly ActorRef<EventLogDatabaseStorageActor.ICommand> databaseStorageActor;
	private readonly IDbContextProvider dbProvider;
	private readonly CancellationToken cancellationToken;

	public EventLogManager(ControllerState controllerState, IActorRefFactory actorSystem, IDbContextProvider dbProvider, CancellationToken cancellationToken) {
		this.controllerState = controllerState;
		this.databaseStorageActor = actorSystem.ActorOf(EventLogDatabaseStorageActor.Factory(new EventLogDatabaseStorageActor.Init(dbProvider, cancellationToken)), "EventLogDatabaseStorage");
		this.dbProvider = dbProvider;
		this.cancellationToken = cancellationToken;
	}

	private void EnqueueItem(Guid eventGuid, DateTime utcTime, Guid? agentGuid, EventLogEventType eventType, string subjectId, Dictionary<string, object?>? extra = null) {
		databaseStorageActor.Tell(new EventLogDatabaseStorageActor.StoreEventCommand(eventGuid, utcTime, agentGuid, eventType, subjectId, extra));
	}
	
	public async Task<Result<ImmutableArray<EventLogItem>, UserActionFailure>> GetMostRecentItems(LoggedInUser loggedInUser, int count) {
		if (!loggedInUser.CheckPermission(Permission.ViewEvents)) {
			return UserActionFailure.NotAuthorized;
		}
		
		var accessibleAgentGuids = loggedInUser.FilterAccessibleAgentGuids(controllerState.AgentsByGuid.Keys.ToImmutableHashSet());
		
		await using var db = dbProvider.Lazy();
		return await new EventLogRepository(db).GetMostRecentItems(accessibleAgentGuids, count, cancellationToken);
	}
}
