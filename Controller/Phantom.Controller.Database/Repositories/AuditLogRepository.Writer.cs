using Phantom.Common.Data.Web.AuditLog;
using Phantom.Controller.Database.Entities;

namespace Phantom.Controller.Database.Repositories;

sealed partial class AuditLogRepository {
	public sealed class ItemWriter {
		private readonly ILazyDbContext db;
		private readonly Guid? currentUserGuid;
		
		internal ItemWriter(ILazyDbContext db, Guid? currentUserGuid) {
			this.db = db;
			this.currentUserGuid = currentUserGuid;
		}
		
		private void AddItem(AuditLogEventType eventType, string subjectId, Dictionary<string, object?>? extra = null) {
			db.Ctx.AuditLog.Add(new AuditLogEntity(currentUserGuid, eventType, subjectId, extra));
		}
		
		public void UserLoggedIn(UserEntity user) {
			AddItem(AuditLogEventType.UserLoggedIn, user.UserGuid.ToString());
		}
		
		public void UserLoggedOut(Guid userGuid) {
			AddItem(AuditLogEventType.UserLoggedOut, userGuid.ToString());
		}
		
		public void AdministratorUserCreated(UserEntity user) {
			AddItem(AuditLogEventType.AdministratorUserCreated, user.UserGuid.ToString());
		}
		
		public void AdministratorUserModified(UserEntity user) {
			AddItem(AuditLogEventType.AdministratorUserCreated, user.UserGuid.ToString());
		}
		
		public void UserCreated(UserEntity user) {
			AddItem(AuditLogEventType.UserCreated, user.UserGuid.ToString());
		}
		
		public void UserPasswordChanged(UserEntity user) {
			AddItem(AuditLogEventType.UserCreated, user.UserGuid.ToString());
		}
		
		public void UserRolesChanged(UserEntity user, List<string> addedToRoles, List<string> removedFromRoles) {
			var extra = new Dictionary<string, object?>();
			
			if (addedToRoles.Count > 0) {
				extra["addedToRoles"] = addedToRoles;
			}
			
			if (removedFromRoles.Count > 0) {
				extra["removedFromRoles"] = removedFromRoles;
			}
			
			AddItem(AuditLogEventType.UserRolesChanged, user.UserGuid.ToString(), extra);
		}
		
		public void UserDeleted(UserEntity user) {
			AddItem(AuditLogEventType.UserDeleted, user.UserGuid.ToString(), new Dictionary<string, object?> {
				{ "username", user.Name },
			});
		}
		
		public void InstanceCreated(Guid instanceGuid) {
			AddItem(AuditLogEventType.InstanceCreated, instanceGuid.ToString());
		}
		
		public void InstanceEdited(Guid instanceGuid) {
			AddItem(AuditLogEventType.InstanceEdited, instanceGuid.ToString());
		}
		
		public void InstanceLaunched(Guid instanceGuid) {
			AddItem(AuditLogEventType.InstanceLaunched, instanceGuid.ToString());
		}
		
		public void InstanceCommandExecuted(Guid instanceGuid, string command) {
			AddItem(AuditLogEventType.InstanceCommandExecuted, instanceGuid.ToString(), new Dictionary<string, object?> {
				{ "command", command },
			});
		}
		
		public void InstanceStopped(Guid instanceGuid, int stopInSeconds) {
			AddItem(AuditLogEventType.InstanceStopped, instanceGuid.ToString(), new Dictionary<string, object?> {
				{ "stop_in_seconds", stopInSeconds.ToString() },
			});
		}
	}
}
