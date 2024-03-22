using System.Collections.Immutable;
using Akka.Actor;
using Phantom.Common.Data.Web.EventLog;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Repositories;
using Phantom.Utils.Actor;

namespace Phantom.Controller.Services.Events; 

sealed partial class EventLogManager {
	private readonly ActorRef<EventLogDatabaseStorageActor.ICommand> databaseStorageActor;
	private readonly IDbContextProvider dbProvider;
	private readonly CancellationToken cancellationToken;

	public EventLogManager(IActorRefFactory actorSystem, IDbContextProvider dbProvider, CancellationToken cancellationToken) {
		this.databaseStorageActor = actorSystem.ActorOf(EventLogDatabaseStorageActor.Factory(new EventLogDatabaseStorageActor.Init(dbProvider, cancellationToken)), "EventLogDatabaseStorage");
		this.dbProvider = dbProvider;
		this.cancellationToken = cancellationToken;
	}

	private void EnqueueItem(Guid eventGuid, DateTime utcTime, Guid? agentGuid, EventLogEventType eventType, string subjectId, Dictionary<string, object?>? extra = null) {
		databaseStorageActor.Tell(new EventLogDatabaseStorageActor.StoreEventCommand(eventGuid, utcTime, agentGuid, eventType, subjectId, extra));
	}
	
	public async Task<ImmutableArray<EventLogItem>> GetMostRecentItems(int count) {
		await using var db = dbProvider.Lazy();
		return await new EventLogRepository(db).GetMostRecentItems(count, cancellationToken);
	}
}
