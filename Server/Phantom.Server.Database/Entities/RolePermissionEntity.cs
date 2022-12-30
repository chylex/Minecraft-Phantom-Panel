using System.ComponentModel.DataAnnotations.Schema;

namespace Phantom.Server.Database.Entities; 

[Table("RolePermissions", Schema = "identity")]
public sealed class RolePermissionEntity {
	public string RoleId { get; set; }
	public string PermissionId { get; set; }
	
	public RolePermissionEntity(string roleId, string permissionId) {
		RoleId = roleId;
		PermissionId = permissionId;
	}
}
