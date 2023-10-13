using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Controller.Database.Entities;

[Table("Users", Schema = "identity")]
public sealed class UserEntity {
	[Key]
	public Guid UserGuid { get; set; }

	public string Name { get; set; }
	public string PasswordHash { get; set; }

	public UserEntity(Guid userGuid, string name, string passwordHash) {
		UserGuid = userGuid;
		Name = name;
		PasswordHash = passwordHash;
	}
	
	public UserInfo ToUserInfo() {
		return new UserInfo(UserGuid, Name);
	}
}
