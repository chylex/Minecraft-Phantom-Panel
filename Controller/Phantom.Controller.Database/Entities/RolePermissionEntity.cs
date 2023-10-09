using System.ComponentModel.DataAnnotations.Schema;

namespace Phantom.Controller.Database.Entities; 

[Table("RolePermissions", Schema = "identity")]
public sealed class RolePermissionEntity {
	public Guid RoleGuid { get; set; }
	public string PermissionId { get; set; }
	
	public RolePermissionEntity(Guid roleGuid, string permissionId) {
		RoleGuid = roleGuid;
		PermissionId = permissionId;
	}
}
