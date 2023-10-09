using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Entities;
using Phantom.Controller.Database.Enums;
using Phantom.Utils.Collections;
using Phantom.Utils.Tasks;

namespace Phantom.Controller.Services.Events;

public sealed partial class EventLog {
	private readonly IDatabaseProvider databaseProvider;
	private readonly TaskManager taskManager;
	private readonly CancellationToken cancellationToken;
	
	public EventLog(IDatabaseProvider databaseProvider, TaskManager taskManager, CancellationToken cancellationToken) {
		this.databaseProvider = databaseProvider;
		this.taskManager = taskManager;
		this.cancellationToken = cancellationToken;
	}

	private async Task AddEntityToDatabase(EventLogEntity logEntity) {
		await using var ctx = databaseProvider.Provide();
		ctx.EventLog.Add(logEntity);
		await ctx.SaveChangesAsync(cancellationToken);
	}

	private void AddItem(Guid eventGuid, DateTime utcTime, Guid? agentGuid, EventLogEventType eventType, string subjectId, Dictionary<string, object?>? extra = null) {
		var logEntity = new EventLogEntity(eventGuid, utcTime, agentGuid, eventType, subjectId, extra);
		taskManager.Run("Store event log item to database", () => AddEntityToDatabase(logEntity));
	}

	public async Task<ImmutableArray<EventLogItem>> GetItems(int count, CancellationToken cancellationToken) {
		await using var ctx = databaseProvider.Provide();
		return await ctx.EventLog
		                .AsQueryable()
		                .OrderByDescending(static entity => entity.UtcTime)
		                .Take(count)
		                .Select(static entity => new EventLogItem(entity.UtcTime, entity.AgentGuid, entity.EventType, entity.SubjectType, entity.SubjectId, entity.Data))
		                .AsAsyncEnumerable()
		                .ToImmutableArrayAsync(cancellationToken);
	}
}
