using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Phantom.Server.Database.Entities; 

[Table("Users", Schema = "identity")]
public sealed class UserEntity {
	[Key]
	public Guid UserGuid { get; set; }

	public string Name { get; set; }
	public string PasswordHash { get; set; }

	public UserEntity(Guid userGuid, string name) {
		UserGuid = userGuid;
		Name = name;
		PasswordHash = null!;
	}
}
