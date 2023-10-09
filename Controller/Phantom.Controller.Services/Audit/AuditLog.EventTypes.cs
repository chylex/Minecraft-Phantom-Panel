using Phantom.Server.Database.Entities;
using Phantom.Server.Database.Enums;

namespace Phantom.Server.Services.Audit;

public sealed partial class AuditLog {
	public Task AddAdministratorUserCreatedEvent(UserEntity administratorUser) {
		return AddItem(AuditLogEventType.AdministratorUserCreated, administratorUser.UserGuid.ToString());
	}

	public Task AddAdministratorUserModifiedEvent(UserEntity administratorUser) {
		return AddItem(AuditLogEventType.AdministratorUserModified, administratorUser.UserGuid.ToString());
	}

	public void AddUserLoggedInEvent(UserEntity user) {
		AddItem(user.UserGuid, AuditLogEventType.UserLoggedIn, user.UserGuid.ToString());
	}

	public void AddUserLoggedOutEvent(Guid userGuid) {
		AddItem(userGuid, AuditLogEventType.UserLoggedOut, userGuid.ToString());
	}
	
	public Task AddUserCreatedEvent(UserEntity user) {
		return AddItem(AuditLogEventType.UserCreated, user.UserGuid.ToString());
	}

	public Task AddUserRolesChangedEvent(UserEntity user, List<string> addedToRoles, List<string> removedFromRoles) {
		var extra = new Dictionary<string, object?>();
		
		if (addedToRoles.Count > 0) {
			extra["addedToRoles"] = addedToRoles;
		}
		
		if (removedFromRoles.Count > 0) {
			extra["removedFromRoles"] = removedFromRoles;
		}
		
		return AddItem(AuditLogEventType.UserRolesChanged, user.UserGuid.ToString(), extra);
	}
	
	public Task AddUserDeletedEvent(UserEntity user) {
		return AddItem(AuditLogEventType.UserDeleted, user.UserGuid.ToString(), new Dictionary<string, object?> {
			{ "username", user.Name }
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
