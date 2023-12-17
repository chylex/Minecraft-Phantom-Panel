using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Data.Web.Users;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Entities;
using Phantom.Controller.Database.Repositories;
using Phantom.Utils.Collections;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Controller.Services.Users;

sealed class RoleManager {
	private static readonly ILogger Logger = PhantomLogger.Create<RoleManager>();

	private readonly IDbContextProvider dbProvider;

	public RoleManager(IDbContextProvider dbProvider) {
		this.dbProvider = dbProvider;
	}

	internal async Task Initialize() {
		Logger.Information("Adding default roles to database.");

		await using var ctx = dbProvider.Eager();

		var existingRoleNames = await ctx.Roles
		                                 .Select(static role => role.Name)
		                                 .AsAsyncEnumerable()
		                                 .ToImmutableSetAsync();

		var existingPermissionIdsByRoleGuid = await ctx.RolePermissions
		                                               .GroupBy(static rp => rp.RoleGuid, static rp => rp.PermissionId)
		                                               .ToDictionaryAsync(static g => g.Key, static g => g.ToImmutableHashSet());

		foreach (var role in Role.All) {
			if (!existingRoleNames.Contains(role.Name)) {
				Logger.Information("Adding default role \"{Name}\".", role.Name);
				ctx.Roles.Add(new RoleEntity(role.Guid, role.Name));
			}

			var existingPermissionIds = existingPermissionIdsByRoleGuid.TryGetValue(role.Guid, out var ids) ? ids : ImmutableHashSet<string>.Empty;
			var missingPermissionIds = PermissionManager.GetMissingPermissionsOrdered(role.Permissions, existingPermissionIds);
			if (!missingPermissionIds.IsEmpty) {
				Logger.Information("Assigning default permission to role \"{Name}\": {Permissions}", role.Name, string.Join(", ", missingPermissionIds));
				foreach (var permissionId in missingPermissionIds) {
					ctx.RolePermissions.Add(new RolePermissionEntity(role.Guid, permissionId));
				}
			}
		}

		await ctx.SaveChangesAsync();
	}

	public async Task<ImmutableArray<RoleInfo>> GetAll() {
		await using var db = dbProvider.Lazy();
		var roleRepository = new RoleRepository(db);
		
		var allRoles = await roleRepository.GetAll();
		return allRoles.Select(static role => role.ToRoleInfo()).ToImmutableArray();
	}
}
