using System.ComponentModel.DataAnnotations.Schema;

namespace Phantom.Controller.Database.Entities;

[Table("UserRoles", Schema = "identity")]
public sealed class UserRoleEntity {
	public Guid UserGuid { get; init; }
	public Guid RoleGuid { get; init; }
	
	public UserEntity User { get; init; }
	public RoleEntity Role { get; init; }
	
	public UserRoleEntity(Guid userGuid, Guid roleGuid) {
		UserGuid = userGuid;
		RoleGuid = roleGuid;
		User = null!;
		Role = null!;
	}
}
