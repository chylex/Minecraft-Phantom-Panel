using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Phantom.Server.Database;
using Phantom.Server.Database.Entities;
using Phantom.Server.Database.Enums;
using Phantom.Server.Services.Users;
using Phantom.Utils.Runtime;

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

	private async Task AddEventToDatabase(AuditEventEntity eventEntity) {
		using var scope = databaseProvider.CreateScope();
		scope.Ctx.AuditEvents.Add(eventEntity);
		await scope.Ctx.SaveChangesAsync(cancellationToken);
	}

	private void AddEvent(string? userId, AuditEventType eventType, string subjectId, Dictionary<string, object?>? extra = null) {
		var eventEntity = new AuditEventEntity(userId, eventType, subjectId, extra);
		taskManager.Run("Store audit log event", () => AddEventToDatabase(eventEntity));
	}

	private async Task AddEvent(AuditEventType eventType, string subjectId, Dictionary<string, object?>? extra = null) {
		AddEvent(await GetCurrentAuthenticatedUserId(), eventType, subjectId, extra);
	}

	public async Task<AuditEvent[]> GetEvents(int count, CancellationToken cancellationToken) {
		using var scope = databaseProvider.CreateScope();
		return await scope.Ctx.AuditEvents
		                  .Include(static entity => entity.User)
		                  .AsQueryable()
		                  .OrderByDescending(static entity => entity.UtcTime)
		                  .Take(count)
		                  .Select(static entity => new AuditEvent(entity.UtcTime, entity.UserId, entity.User == null ? null : entity.User.UserName, entity.EventType, entity.SubjectType, entity.SubjectId, entity.Data))
		                  .ToArrayAsync(cancellationToken);
	}
}
