using Phantom.Common.Data.Web.EventLog;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Repositories;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Controller.Services.Events;

sealed class EventLogDatabaseStorageActor : ReceiveActor<EventLogDatabaseStorageActor.ICommand> {
	private static readonly ILogger Logger = PhantomLogger.Create<EventLogDatabaseStorageActor>();
	
	public readonly record struct Init(IDbContextProvider DbProvider, CancellationToken CancellationToken);
	
	public static Props<ICommand> Factory(Init init) {
		return Props<ICommand>.Create(() => new EventLogDatabaseStorageActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume });
	}

	private readonly IDbContextProvider dbProvider;
	private readonly CancellationToken cancellationToken;
	
	private readonly LinkedList<StoreEventCommand> pendingCommands = new ();
	private bool hasScheduledFlush = false;
	
	private EventLogDatabaseStorageActor(Init init) {
		this.dbProvider = init.DbProvider;
		this.cancellationToken = init.CancellationToken;
		
		Receive<StoreEventCommand>(StoreEvent);
		ReceiveAsync<FlushChangesCommand>(FlushChanges);
	}

	public interface ICommand {}

	public sealed record StoreEventCommand(Guid EventGuid, DateTime UtcTime, Guid? AgentGuid, EventLogEventType EventType, string SubjectId, Dictionary<string, object?>? Extra = null) : ICommand;
	
	private sealed record FlushChangesCommand : ICommand;

	private void StoreEvent(StoreEventCommand command) {
		pendingCommands.AddLast(command);
		ScheduleFlush(TimeSpan.FromMilliseconds(500));
	}

	private async Task FlushChanges(FlushChangesCommand command) {
		hasScheduledFlush = false;
		
		if (pendingCommands.Count == 0) {
			return;
		}

		try {
			await using var db = dbProvider.Lazy();
			var eventLogRepository = new EventLogRepository(db);
			
			foreach (var (eventGuid, dateTime, agentGuid, eventLogEventType, subjectId, extra) in pendingCommands) {
				eventLogRepository.AddItem(eventGuid, dateTime, agentGuid, eventLogEventType, subjectId, extra);
			}
			
			await db.Ctx.SaveChangesAsync(cancellationToken);
		} catch (Exception e) {
			ScheduleFlush(TimeSpan.FromSeconds(10));
			Logger.Error(e, "Could not store {EventCount} event(s) in database.", pendingCommands.Count);
			return;
		}
		
		Logger.Information("Stored {EventCount} event(s) in database.", pendingCommands.Count);
		
		pendingCommands.Clear();
	}

	private void ScheduleFlush(TimeSpan delay) {
		if (!hasScheduledFlush) {
			hasScheduledFlush = true;
			Context.System.Scheduler.ScheduleTellOnce(delay, Self, new FlushChangesCommand(), Self);
		}
	}
}
