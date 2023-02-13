using Microsoft.AspNetCore.Identity;
using Phantom.Server.Database.Enums;

namespace Phantom.Server.Services.Audit;

public sealed partial class AuditLog {
	public Task AddAdministratorUserCreatedEvent(IdentityUser administratorUser) {
		return AddItem(AuditLogEventType.AdministratorUserCreated, administratorUser.Id);
	}

	public Task AddAdministratorUserModifiedEvent(IdentityUser administratorUser) {
		return AddItem(AuditLogEventType.AdministratorUserModified, administratorUser.Id);
	}

	public void AddUserLoggedInEvent(string userId) {
		AddItem(userId, AuditLogEventType.UserLoggedIn, userId);
	}

	public void AddUserLoggedOutEvent(string userId) {
		AddItem(userId, AuditLogEventType.UserLoggedOut, userId);
	}
	
	public Task AddUserCreatedEvent(IdentityUser user) {
		return AddItem(AuditLogEventType.UserCreated, user.Id);
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
		
		return AddItem(AuditLogEventType.UserDeleted, user.Id, extra);
	}
	
	public Task AddUserDeletedEvent(IdentityUser user) {
		return AddItem(AuditLogEventType.UserDeleted, user.Id, new Dictionary<string, object?> {
			{ "username", user.UserName }
		});
	}

	public Task AddInstanceCreatedEvent(Guid instanceGuid) {
		return AddItem(AuditLogEventType.InstanceCreated, instanceGuid.ToString());
	}

	public Task AddInstanceEditedEvent(Guid instanceGuid) {
		return AddItem(AuditLogEventType.InstanceEdited, instanceGuid.ToString());
	}
	
	public Task AddInstanceLaunchedEvent(Guid instanceGuid) {
		return AddItem(AuditLogEventType.InstanceLaunched, instanceGuid.ToString());
	}

	public Task AddInstanceCommandExecutedEvent(Guid instanceGuid, string command) {
		return AddItem(AuditLogEventType.InstanceCommandExecuted, instanceGuid.ToString(), new Dictionary<string, object?> {
			{ "command", command }
		});
	}

	public Task AddInstanceStoppedEvent(Guid instanceGuid, int stopInSeconds) {
		return AddItem(AuditLogEventType.InstanceStopped, instanceGuid.ToString(), new Dictionary<string, object?> {
			{ "stop_in_seconds", stopInSeconds.ToString() }
		});
	}
}
