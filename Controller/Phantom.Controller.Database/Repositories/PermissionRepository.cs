using Microsoft.EntityFrameworkCore;
using Phantom.Common.Data.Web.Users;
using Phantom.Controller.Database.Entities;
using Phantom.Utils.Collections;

namespace Phantom.Controller.Database.Repositories;

public sealed class PermissionRepository {
	private readonly ILazyDbContext db;

	public PermissionRepository(ILazyDbContext db) {
		this.db = db;
	}

	public async Task<PermissionSet> GetAllUserPermissions(UserEntity user) {
		var userPermissions = db.Ctx.UserPermissions
		                        .Where(up => up.UserGuid == user.UserGuid)
		                        .Select(static up => up.PermissionId);

		var rolePermissions = db.Ctx.UserRoles
		                        .Where(ur => ur.UserGuid == user.UserGuid)
		                        .Join(db.Ctx.RolePermissions, static ur => ur.RoleGuid, static rp => rp.RoleGuid, static (ur, rp) => rp.PermissionId);

		return new PermissionSet(await userPermissions.Union(rolePermissions).AsAsyncEnumerable().ToImmutableSetAsync());
	}
}
