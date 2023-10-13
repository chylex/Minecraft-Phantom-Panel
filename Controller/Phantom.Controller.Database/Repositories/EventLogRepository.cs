using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Data.Web.EventLog;
using Phantom.Controller.Database.Entities;
using Phantom.Utils.Collections;

namespace Phantom.Controller.Database.Repositories;

public sealed class EventLogRepository {
	private readonly ILazyDbContext db;

	public EventLogRepository(ILazyDbContext db) {
		this.db = db;
	}

	public void AddItem(Guid eventGuid, DateTime utcTime, Guid? agentGuid, EventLogEventType eventType, string subjectId, Dictionary<string, object?>? extra = null) {
		db.Ctx.EventLog.Add(new EventLogEntity(eventGuid, utcTime, agentGuid, eventType, subjectId, extra));
	}
	
	public Task<ImmutableArray<EventLogItem>> GetMostRecentItems(int count, CancellationToken cancellationToken) {
		return db.Ctx
		         .EventLog
		         .AsQueryable()
		         .OrderByDescending(static entity => entity.UtcTime)
		         .Take(count)
		         .AsAsyncEnumerable()
		         .Select(static entity => new EventLogItem(entity.UtcTime, entity.AgentGuid, entity.EventType, entity.SubjectType, entity.SubjectId, entity.Data?.RootElement.ToString()))
		         .ToImmutableArrayAsync(cancellationToken);
	}
}
