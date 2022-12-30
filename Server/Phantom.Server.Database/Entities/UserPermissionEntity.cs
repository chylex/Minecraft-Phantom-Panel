using System.ComponentModel.DataAnnotations.Schema;

namespace Phantom.Server.Database.Entities;

[Table("UserPermissions", Schema = "identity")]
public sealed class UserPermissionEntity {
	public string UserId { get; set; }
	public string PermissionId { get; set; }

	public UserPermissionEntity(string userId, string permissionId) {
		UserId = userId;
		PermissionId = permissionId;
	}
}
