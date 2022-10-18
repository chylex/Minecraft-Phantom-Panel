﻿using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Phantom.Server.Database.Enums;

namespace Phantom.Server.Services.Audit;

public sealed partial class AuditLog {
	public Task AddAdministratorUserCreatedEvent(IdentityUser administratorUser) {
		return AddEvent(AuditEventType.AdministratorUserCreated, administratorUser.Id);
	}

	public Task AddAdministratorUserModifiedEvent(IdentityUser administratorUser) {
		return AddEvent(AuditEventType.AdministratorUserModified, administratorUser.Id);
	}

	public void AddUserLoggedInEvent(string userId) {
		AddEvent(userId, AuditEventType.UserLoggedIn, userId);
	}

	public void AddUserLoggedOutEvent(ClaimsPrincipal user) {
		var userId = GetUserId(user);
		AddEvent(userId, AuditEventType.UserLoggedOut, userId ?? string.Empty);
	}

	public Task AddInstanceCreatedEvent(Guid instanceGuid) {
		return AddEvent(AuditEventType.InstanceCreated, instanceGuid.ToString());
	}

	public Task AddInstanceLaunchedEvent(Guid instanceGuid) {
		return AddEvent(AuditEventType.InstanceLaunched, instanceGuid.ToString());
	}

	public Task AddInstanceCommandExecutedEvent(Guid instanceGuid, string command) {
		return AddEvent(AuditEventType.InstanceCommandExecuted, instanceGuid.ToString(), new Dictionary<string, object?> {
			{ "command", command }
		});
	}

	public Task AddInstanceStoppedEvent(Guid instanceGuid, int stopInSeconds) {
		return AddEvent(AuditEventType.InstanceStopped, instanceGuid.ToString(), new Dictionary<string, object?> {
			{ "stop_in_seconds", stopInSeconds.ToString() }
		});
	}
}