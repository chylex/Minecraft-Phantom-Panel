using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Phantom.Server.Database;
using Phantom.Server.Database.Entities;
using Phantom.Server.Database.Enums;
using Phantom.Utils.Runtime;

namespace Phantom.Server.Services.Audit;

public sealed partial class AuditLog {
	private readonly CancellationToken cancellationToken;
	private readonly DatabaseProvider databaseProvider;
	private readonly AuthenticationStateProvider authenticationStateProvider;
	private readonly TaskManager taskManager;

	public AuditLog(ServiceConfiguration serviceConfiguration, DatabaseProvider databaseProvider, AuthenticationStateProvider authenticationStateProvider, TaskManager taskManager) {
		this.cancellationToken = serviceConfiguration.CancellationToken;
		this.databaseProvider = databaseProvider;
		this.authenticationStateProvider = authenticationStateProvider;
		this.taskManager = taskManager;
	}

	private static string? GetUserId(ClaimsPrincipal user) {
		return user.FindFirstValue(ClaimTypes.NameIdentifier);
	}

	private async Task<string?> GetCurrentUserId() {
		var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
		return authenticationState.User.Identity?.IsAuthenticated == true ? GetUserId(authenticationState.User) : null;
	}

	private async Task AddEventToDatabase(AuditEventEntity eventEntity) {
		using var scope = databaseProvider.CreateScope();
		scope.Ctx.AuditEvents.Add(eventEntity);
		await scope.Ctx.SaveChangesAsync(cancellationToken);
	}

	private void AddEvent(string? userId, AuditEventType eventType, string subjectId, Dictionary<string, object?>? extra = null) {
		var eventEntity = new AuditEventEntity(userId, eventType, subjectId, extra);
		taskManager.Run(() => AddEventToDatabase(eventEntity));
	}

	private async Task AddEvent(AuditEventType eventType, string subjectId, Dictionary<string, object?>? extra = null) {
		AddEvent(await GetCurrentUserId(), eventType, subjectId, extra);
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
