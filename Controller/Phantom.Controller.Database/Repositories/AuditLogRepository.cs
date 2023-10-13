using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Data.Web.AuditLog;
using Phantom.Utils.Collections;

namespace Phantom.Controller.Database.Repositories;

public sealed partial class AuditLogRepository {
	private readonly ILazyDbContext db;

	public AuditLogRepository(ILazyDbContext db) {
		this.db = db;
	}

	public Task<ImmutableArray<AuditLogItem>> GetMostRecentItems(int count, CancellationToken cancellationToken) {
		return db.Ctx
		         .AuditLog
		         .Include(static entity => entity.User)
		         .AsQueryable()
		         .OrderByDescending(static entity => entity.UtcTime)
		         .Take(count)
		         .AsAsyncEnumerable()
		         .Select(static entity => new AuditLogItem(entity.UtcTime, entity.UserGuid, entity.User?.Name, entity.EventType, entity.SubjectType, entity.SubjectId, entity.Data?.RootElement.ToString()))
		         .ToImmutableArrayAsync(cancellationToken);
	}
	
	public ItemWriter Writer(Guid? currentUserGuid) {
		return new ItemWriter(db, currentUserGuid);
	}
}
