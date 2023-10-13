﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Controller.Database.Entities;

[Table("Roles", Schema = "identity")]
public sealed class RoleEntity {
	[Key]
	public Guid RoleGuid { get; set; }

	public string Name { get; set; }

	public RoleEntity(Guid roleGuid, string name) {
		RoleGuid = roleGuid;
		Name = name;
	}

	public RoleInfo ToRoleInfo() {
		return new RoleInfo(RoleGuid, Name);
	}
}
