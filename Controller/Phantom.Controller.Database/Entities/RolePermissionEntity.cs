using System.ComponentModel.DataAnnotations.Schema;

namespace Phantom.Controller.Database.Entities;

[Table("RolePermissions", Schema = "identity")]
public sealed class RolePermissionEntity {
	public Guid RoleGuid { get; init; }
	public string PermissionId { get; init; }
	
	public RolePermissionEntity(Guid roleGuid, string permissionId) {
		RoleGuid = roleGuid;
		PermissionId = permissionId;
	}
}
