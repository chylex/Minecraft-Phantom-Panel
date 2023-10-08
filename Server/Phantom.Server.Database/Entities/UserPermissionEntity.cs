using System.ComponentModel.DataAnnotations.Schema;

namespace Phantom.Server.Database.Entities;

[Table("UserPermissions", Schema = "identity")]
public sealed class UserPermissionEntity {
	public Guid UserGuid { get; set; }
	public string PermissionId { get; set; }

	public UserPermissionEntity(Guid userGuid, string permissionId) {
		UserGuid = userGuid;
		PermissionId = permissionId;
	}
}
