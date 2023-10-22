using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Phantom.Controller.Database.Entities;

[Table("Permissions", Schema = "identity")]
public sealed class PermissionEntity {
	[Key]
	public string Id { get; set; }

	public PermissionEntity(string id) {
		Id = id;
	}
}
