using System.ComponentModel.DataAnnotations.Schema;

namespace Phantom.Controller.Database.Entities;

[Table("UserPermissions", Schema = "identity")]
public sealed class UserPermissionEntity {
	public Guid UserGuid { get; init; }
	public string PermissionId { get; init; }
	
	public UserPermissionEntity(Guid userGuid, string permissionId) {
		UserGuid = userGuid;
		PermissionId = permissionId;
	}
}
