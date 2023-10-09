using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Logging;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Entities;
using Phantom.Controller.Services.Users.Permissions;
using Phantom.Utils.Collections;
using Phantom.Utils.Tasks;
using ILogger = Serilog.ILogger;

namespace Phantom.Controller.Services.Users.Roles;

public sealed class RoleManager {
	private static readonly ILogger Logger = PhantomLogger.Create<RoleManager>();

	private const int MaxRoleNameLength = 40;

	private readonly IDatabaseProvider databaseProvider;

	public RoleManager(IDatabaseProvider databaseProvider) {
		this.databaseProvider = databaseProvider;
	}

	internal async Task Initialize() {
		Logger.Information("Adding default roles to database.");
		
		await using var ctx = databaseProvider.Provide();

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

	public async Task<List<RoleEntity>> GetAll() {
		await using var ctx = databaseProvider.Provide();
		return await ctx.Roles.ToListAsync();
	}

	public async Task<ImmutableHashSet<string>> GetAllNames() {
		await using var ctx = databaseProvider.Provide();
		return await ctx.Roles.Select(static role => role.Name).AsAsyncEnumerable().ToImmutableSetAsync();
	}

	public async ValueTask<RoleEntity?> GetByGuid(Guid guid) {
		await using var ctx = databaseProvider.Provide();
		return await ctx.Roles.FindAsync(guid);
	}

	public async Task<Result<RoleEntity, AddRoleError>> Create(string name) {
		if (string.IsNullOrWhiteSpace(name)) {
			return Result.Fail<RoleEntity, AddRoleError>(AddRoleError.NameIsEmpty);
		}
		else if (name.Length > MaxRoleNameLength) {
			return Result.Fail<RoleEntity, AddRoleError>(AddRoleError.NameIsTooLong);
		}

		RoleEntity newRole;
		try {
			await using var ctx = databaseProvider.Provide();
			
			if (await ctx.Roles.AnyAsync(role => role.Name == name)) {
				return Result.Fail<RoleEntity, AddRoleError>(AddRoleError.NameAlreadyExists);
			}
				
			newRole = new RoleEntity(Guid.NewGuid(), name);
			ctx.Roles.Add(newRole);
			await ctx.SaveChangesAsync();
		} catch (Exception e) {
			Logger.Error(e, "Could not create role \"{Name}\".", name);
			return Result.Fail<RoleEntity, AddRoleError>(AddRoleError.UnknownError);
		}

		Logger.Information("Created role \"{Name}\" (GUID {Guid}).", name, newRole.RoleGuid);
		return Result.Ok<RoleEntity, AddRoleError>(newRole);
	}
}
