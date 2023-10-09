using Microsoft.EntityFrameworkCore;
using Phantom.Server.Database;
using Phantom.Server.Database.Entities;
using Phantom.Server.Database.Enums;
using Phantom.Utils.Tasks;

namespace Phantom.Server.Services.Events; 

public sealed partial class EventLog {
	private readonly CancellationToken cancellationToken;
	private readonly DatabaseProvider databaseProvider;
	private readonly TaskManager taskManager;
	
	public EventLog(ServiceConfiguration serviceConfiguration, DatabaseProvider databaseProvider, TaskManager taskManager) {
		this.cancellationToken = serviceConfiguration.CancellationToken;
		this.databaseProvider = databaseProvider;
		this.taskManager = taskManager;
	}

	private async Task AddEntityToDatabase(EventLogEntity logEntity) {
		using var scope = databaseProvider.CreateScope();
		scope.Ctx.EventLog.Add(logEntity);
		await scope.Ctx.SaveChangesAsync(cancellationToken);
	}

	private void AddItem(Guid eventGuid, DateTime utcTime, Guid? agentGuid, EventLogEventType eventType, string subjectId, Dictionary<string, object?>? extra = null) {
		var logEntity = new EventLogEntity(eventGuid, utcTime, agentGuid, eventType, subjectId, extra);
		taskManager.Run("Store event log item to database", () => AddEntityToDatabase(logEntity));
	}

	public async Task<EventLogItem[]> GetItems(int count, CancellationToken cancellationToken) {
		using var scope = databaseProvider.CreateScope();
		return await scope.Ctx.EventLog
		                  .AsQueryable()
		                  .OrderByDescending(static entity => entity.UtcTime)
		                  .Take(count)
		                  .Select(static entity => new EventLogItem(entity.UtcTime, entity.AgentGuid, entity.EventType, entity.SubjectType, entity.SubjectId, entity.Data))
		                  .ToArrayAsync(cancellationToken);
	}
}
