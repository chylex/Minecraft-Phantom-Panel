using System.Collections.Immutable;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Phantom.Common.Logging;
using Phantom.Server.Database;
using Phantom.Server.Database.Entities;
using Phantom.Server.Web.Identity.Data;
using Phantom.Utils.Runtime;
using Serilog;

namespace Phantom.Server.Web.Identity;

public sealed class PhantomIdentityConfigurator {
	private static readonly ILogger Logger = PhantomLogger.Create<PhantomIdentityConfigurator>();

	public static async Task MigrateDatabase(IServiceProvider serviceProvider) {
		await using var scope = serviceProvider.CreateAsyncScope();
		await scope.ServiceProvider.GetRequiredService<PhantomIdentityConfigurator>().Initialize();
	}

	private readonly ApplicationDbContext db;
	private readonly RoleManager<IdentityRole> roleManager;

	public PhantomIdentityConfigurator(ApplicationDbContext db, RoleManager<IdentityRole> roleManager) {
		this.db = db;
		this.roleManager = roleManager;
	}

	private async Task Initialize() {
		CreatePermissions();
		await CreateDefaultRoles();
		await AssignDefaultRolePermissions();
		await db.SaveChangesAsync();
	}

	private void CreatePermissions() {
		var existingPermissionIds = db.Permissions.Select(static p => p.Id).ToHashSet();
		var missingPermissionIds = GetMissingPermissionsOrdered(Permission.All, existingPermissionIds);

		if (!missingPermissionIds.IsEmpty) {
			Logger.Information("Adding permissions: {Permissions}", string.Join(", ", missingPermissionIds));
			foreach (var permissionId in missingPermissionIds) {
				db.Permissions.Add(new PermissionEntity(permissionId));
			}
		}
	}

	private async Task CreateDefaultRoles() {
		foreach (var role in Role.All) {
			string name = role.Name;
			if (await roleManager.RoleExistsAsync(name)) {
				continue;
			}

			Logger.Information("Creating default role {RoleName}.", name);
			var result = await roleManager.CreateAsync(new IdentityRole(name));

			if (!result.Succeeded) {
				bool anyError = false;
				
				foreach (var error in result.Errors) {
					Logger.Fatal("Error creating default role {RoleName}: {Error}", name, error.Description);
					anyError = true;
				}

				if (!anyError) {
					Logger.Fatal("Error creating default role {RoleName} due to unknown error.", name);
				}

				throw StopProcedureException.Instance;
			}
		}
	}

	private async Task AssignDefaultRolePermissions() {
		Logger.Information("Assigning default role permissions..");
		
		foreach (var role in Role.All) {
			var roleEntity = await roleManager.FindByNameAsync(role.Name);
			if (roleEntity == null) {
				Logger.Fatal("Error assigning default role permissions, role {RoleName} not found.", role.Name);
				throw StopProcedureException.Instance;
			}
			
			var existingPermissionIds = db.RolePermissions.Where(rp => rp.RoleId == roleEntity.Id).Select(static rp => rp.PermissionId).ToHashSet();
			var missingPermissionIds = GetMissingPermissionsOrdered(role.Permissions, existingPermissionIds);
			
			if (!missingPermissionIds.IsEmpty) {
				Logger.Information("Assigning default permission to role {RoleName}: {Permissions}", role.Name, string.Join(", ", missingPermissionIds));
				foreach (var permissionId in missingPermissionIds) {
					db.RolePermissions.Add(new RolePermissionEntity(roleEntity.Id, permissionId));
				}
			}
		}
	}

	private static ImmutableArray<string> GetMissingPermissionsOrdered(IEnumerable<Permission> allPermissions, HashSet<string> existingPermissionIds) {
		return allPermissions.Select(static permission => permission.Id).Except(existingPermissionIds).Order().ToImmutableArray();
	}
}
