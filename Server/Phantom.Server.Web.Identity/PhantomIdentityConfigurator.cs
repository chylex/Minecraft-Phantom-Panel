using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Logging;
using Phantom.Server.Database;
using Phantom.Server.Database.Entities;
using Phantom.Server.Services.Users;
using Phantom.Server.Web.Identity.Data;
using Phantom.Utils.Collections;
using Phantom.Utils.Runtime;
using Phantom.Utils.Tasks;
using ILogger = Serilog.ILogger;

namespace Phantom.Server.Web.Identity;

public sealed class PhantomIdentityConfigurator {
	private static readonly ILogger Logger = PhantomLogger.Create<PhantomIdentityConfigurator>();

	public static async Task MigrateDatabase(IServiceProvider serviceProvider) {
		await using var scope = serviceProvider.CreateAsyncScope();
		await scope.ServiceProvider.GetRequiredService<PhantomIdentityConfigurator>().Initialize();
	}

	private readonly ApplicationDbContext db;
	private readonly RoleManager roleManager;

	public PhantomIdentityConfigurator(ApplicationDbContext db, RoleManager roleManager) {
		this.db = db;
		this.roleManager = roleManager;
	}

	private async Task Initialize() {
		await CreatePermissions();
		await CreateDefaultRoles();
		await AssignDefaultRolePermissions();
		await db.SaveChangesAsync();
	}

	private async Task CreatePermissions() {
		var existingPermissionIds = await db.Permissions.Select(static p => p.Id).AsAsyncEnumerable().ToImmutableSetAsync();
		var missingPermissionIds = GetMissingPermissionsOrdered(Permission.All, existingPermissionIds);

		if (!missingPermissionIds.IsEmpty) {
			Logger.Information("Adding permissions: {Permissions}", string.Join(", ", missingPermissionIds));
			foreach (var permissionId in missingPermissionIds) {
				db.Permissions.Add(new PermissionEntity(permissionId));
			}
		}
	}

	private async Task CreateDefaultRoles() {
		Logger.Information("Creating default roles.");
		
		var allRoleNames = await roleManager.GetAllNames();
		
		foreach (var (guid, name, _) in Role.All) {
			if (allRoleNames.Contains(name)) {
				continue;
			}
			
			var result = await roleManager.Create(guid, name);
			if (result is Result<RoleEntity, AddRoleError>.Fail fail) {
				switch (fail.Error) {
					case AddRoleError.NameIsEmpty:
						Logger.Fatal("Error creating default role \"{Name}\", name is empty!", name);
						throw StopProcedureException.Instance;
					
					case AddRoleError.NameIsTooLong:
						Logger.Fatal("Error creating default role \"{Name}\", name is too long!", name);
						throw StopProcedureException.Instance;
					
					case AddRoleError.NameAlreadyExists:
						Logger.Warning("Error creating default role \"{Name}\", a role with this name already exists!", name);
						throw StopProcedureException.Instance;
					
					default:
						Logger.Fatal("Error creating default role \"{Name}\", unknown error!", name);
						throw StopProcedureException.Instance;
				}
			}
		}
	}

	private async Task AssignDefaultRolePermissions() {
		Logger.Information("Assigning default role permissions.");
		
		foreach (var role in Role.All) {
			var roleEntity = await roleManager.GetByGuid(role.Guid);
			if (roleEntity == null) {
				Logger.Fatal("Error assigning default role permissions, role \"{Name}\" with GUID {Guid} not found.", role.Name, role.Guid);
				throw StopProcedureException.Instance;
			}
			
			var existingPermissionIds = await db.RolePermissions
			                                    .Where(rp => rp.RoleGuid == roleEntity.RoleGuid)
			                                    .Select(static rp => rp.PermissionId)
			                                    .AsAsyncEnumerable()
			                                    .ToImmutableSetAsync();
            
			var missingPermissionIds = GetMissingPermissionsOrdered(role.Permissions, existingPermissionIds);
			if (!missingPermissionIds.IsEmpty) {
				Logger.Information("Assigning default permission to role \"{Name}\": {Permissions}", role.Name, string.Join(", ", missingPermissionIds));
				foreach (var permissionId in missingPermissionIds) {
					db.RolePermissions.Add(new RolePermissionEntity(roleEntity.RoleGuid, permissionId));
				}
			}
		}
	}

	private static ImmutableArray<string> GetMissingPermissionsOrdered(IEnumerable<Permission> allPermissions, ImmutableHashSet<string> existingPermissionIds) {
		return allPermissions.Select(static permission => permission.Id).Except(existingPermissionIds).Order().ToImmutableArray();
	}
}
