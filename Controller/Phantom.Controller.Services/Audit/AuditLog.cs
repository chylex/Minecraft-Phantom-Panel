using Microsoft.EntityFrameworkCore;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Entities;
using Phantom.Controller.Database.Enums;
using Phantom.Utils.Tasks;

namespace Phantom.Controller.Services.Audit;

public sealed partial class AuditLog {
	private readonly IDatabaseProvider databaseProvider;
	private readonly TaskManager taskManager;
	private readonly CancellationToken cancellationToken;

	public AuditLog(IDatabaseProvider databaseProvider, TaskManager taskManager, CancellationToken cancellationToken) {
		this.databaseProvider = databaseProvider;
		this.taskManager = taskManager;
		this.cancellationToken = cancellationToken;
	}
	
	private Task<Guid?> GetCurrentAuthenticatedUserId() {
		return Task.FromResult<Guid?>(null); // TODO
	}

	private async Task AddEntityToDatabase(AuditLogEntity logEntity) {
		await using var ctx = databaseProvider.Provide();
		ctx.AuditLog.Add(logEntity);
		await ctx.SaveChangesAsync(cancellationToken);
	}

	private void AddItem(Guid? userGuid, AuditLogEventType eventType, string subjectId, Dictionary<string, object?>? extra = null) {
		var logEntity = new AuditLogEntity(userGuid, eventType, subjectId, extra);
		taskManager.Run("Store audit log item to database", () => AddEntityToDatabase(logEntity));
	}

	private async Task AddItem(AuditLogEventType eventType, string subjectId, Dictionary<string, object?>? extra = null) {
		AddItem(await GetCurrentAuthenticatedUserId(), eventType, subjectId, extra);
	}

	public async Task<AuditLogItem[]> GetItems(int count, CancellationToken cancellationToken) {
		await using var ctx = databaseProvider.Provide();
		return await ctx.AuditLog
		                .Include(static entity => entity.User)
		                .AsQueryable()
		                .OrderByDescending(static entity => entity.UtcTime)
		                .Take(count)
		                .Select(static entity => new AuditLogItem(entity.UtcTime, entity.UserGuid, entity.User == null ? null : entity.User.Name, entity.EventType, entity.SubjectType, entity.SubjectId, entity.Data))
		                .ToArrayAsync(cancellationToken);
	}
}
