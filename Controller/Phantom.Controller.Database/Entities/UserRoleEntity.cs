using System.ComponentModel.DataAnnotations.Schema;

namespace Phantom.Controller.Database.Entities;

[Table("UserRoles", Schema = "identity")]
public sealed class UserRoleEntity {
	public Guid UserGuid { get; set; }
	public Guid RoleGuid { get; set; }

	public UserEntity User { get; set; }
	public RoleEntity Role { get; set; }

	public UserRoleEntity(Guid userGuid, Guid roleGuid) {
		UserGuid = userGuid;
		RoleGuid = roleGuid;
		User = null!;
		Role = null!;
	}
}
