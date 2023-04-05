using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Phantom.Server.Database;
using Phantom.Server.Database.Entities;
using Phantom.Server.Database.Enums;
using Phantom.Server.Services.Users;
using Phantom.Utils.Tasks;

namespace Phantom.Server.Services.Audit;

public sealed partial class AuditLog {
	private readonly CancellationToken cancellationToken;
	private readonly DatabaseProvider databaseProvider;
	private readonly IdentityLookup identityLookup;
	private readonly AuthenticationStateProvider authenticationStateProvider;
	private readonly TaskManager taskManager;

	public AuditLog(ServiceConfiguration serviceConfiguration, DatabaseProvider databaseProvider, IdentityLookup identityLookup, AuthenticationStateProvider authenticationStateProvider, TaskManager taskManager) {
		this.cancellationToken = serviceConfiguration.CancellationToken;
		this.databaseProvider = databaseProvider;
		this.identityLookup = identityLookup;
		this.authenticationStateProvider = authenticationStateProvider;
		this.taskManager = taskManager;
	}
	
	private async Task<string?> GetCurrentAuthenticatedUserId() {
		var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
		return identityLookup.GetAuthenticatedUserId(authenticationState.User);
	}

	private async Task AddEntityToDatabase(AuditLogEntity logEntity) {
		using var scope = databaseProvider.CreateScope();
		scope.Ctx.AuditLog.Add(logEntity);
		await scope.Ctx.SaveChangesAsync(cancellationToken);
	}

	private void AddItem(string? userId, AuditLogEventType eventType, string subjectId, Dictionary<string, object?>? extra = null) {
		var logEntity = new AuditLogEntity(userId, eventType, subjectId, extra);
		taskManager.Run("Store audit log item to database", () => AddEntityToDatabase(logEntity));
	}

	private async Task AddItem(AuditLogEventType eventType, string subjectId, Dictionary<string, object?>? extra = null) {
		AddItem(await GetCurrentAuthenticatedUserId(), eventType, subjectId, extra);
	}

	public async Task<AuditLogItem[]> GetItems(int count, CancellationToken cancellationToken) {
		using var scope = databaseProvider.CreateScope();
		return await scope.Ctx.AuditLog
		                  .Include(static entity => entity.User)
		                  .AsQueryable()
		                  .OrderByDescending(static entity => entity.UtcTime)
		                  .Take(count)
		                  .Select(static entity => new AuditLogItem(entity.UtcTime, entity.UserId, entity.User == null ? null : entity.User.UserName, entity.EventType, entity.SubjectType, entity.SubjectId, entity.Data))
		                  .ToArrayAsync(cancellationToken);
	}
}
