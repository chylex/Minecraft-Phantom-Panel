﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Phantom.Server.Database.Entities; 

[Table("Permissions", Schema = "identity")]
public class PermissionEntity {
	[Key]
	public string Id { get; set; }

	public PermissionEntity(string id) {
		Id = id;
	}
}
