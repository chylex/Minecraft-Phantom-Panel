using System.Security.Claims;
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
		var userId = identityLookup.GetAuthenticatedUserId(user);
		AddEvent(userId, AuditEventType.UserLoggedOut, userId ?? string.Empty);
	}
	
	public Task AddUserCreatedEvent(IdentityUser user) {
		return AddEvent(AuditEventType.UserCreated, user.Id);
	}

	public Task AddUserRolesChangedEvent(IdentityUser user, List<string> addedToRoles, List<string> removedFromRoles) {
		var extra = new Dictionary<string, object?> {
			{ "username", user.UserName },
		};
		
		if (addedToRoles.Count > 0) {
			extra["addedToRoles"] = addedToRoles;
		}
		
		if (removedFromRoles.Count > 0) {
			extra["removedFromRoles"] = removedFromRoles;
		}
		
		return AddEvent(AuditEventType.UserDeleted, user.Id, extra);
	}
	
	public Task AddUserDeletedEvent(IdentityUser user) {
		return AddEvent(AuditEventType.UserDeleted, user.Id, new Dictionary<string, object?> {
			{ "username", user.UserName }
		});
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
