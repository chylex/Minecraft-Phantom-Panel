using System.Collections.Immutable;
using Phantom.Common.Data.Web.EventLog;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Repositories;
using Phantom.Utils.Tasks;

namespace Phantom.Controller.Services.Events; 

sealed partial class EventLogManager {
	private readonly IDbContextProvider dbProvider;
	private readonly TaskManager taskManager;
	private readonly CancellationToken cancellationToken;

	public EventLogManager(IDbContextProvider dbProvider, TaskManager taskManager, CancellationToken cancellationToken) {
		this.dbProvider = dbProvider;
		this.taskManager = taskManager;
		this.cancellationToken = cancellationToken;
	}

	public void EnqueueItem(Guid eventGuid, DateTime utcTime, Guid? agentGuid, EventLogEventType eventType, string subjectId, Dictionary<string, object?>? extra = null) {
		taskManager.Run("Store event log item to database", () => AddItem(eventGuid, utcTime, agentGuid, eventType, subjectId, extra));
	}
	
	public async Task AddItem(Guid eventGuid, DateTime utcTime, Guid? agentGuid, EventLogEventType eventType, string subjectId, Dictionary<string, object?>? extra = null) {
		await using var db = dbProvider.Lazy();
		new EventLogRepository(db).AddItem(eventGuid, utcTime, agentGuid, eventType, subjectId, extra);
		await db.Ctx.SaveChangesAsync(cancellationToken);
	}

	public async Task<ImmutableArray<EventLogItem>> GetMostRecentItems(int count) {
		await using var db = dbProvider.Lazy();
		return await new EventLogRepository(db).GetMostRecentItems(count, cancellationToken);
	}
}
