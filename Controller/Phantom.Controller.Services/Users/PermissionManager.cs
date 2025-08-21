using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Data.Web.Users;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Entities;
using Phantom.Utils.Collections;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Controller.Services.Users;

sealed class PermissionManager {
	private static readonly ILogger Logger = PhantomLogger.Create<PermissionManager>();
	
	private readonly IDbContextProvider dbProvider;
	
	public PermissionManager(IDbContextProvider dbProvider) {
		this.dbProvider = dbProvider;
	}
	
	public async Task Initialize() {
		Logger.Information("Adding default permissions to database.");
		
		await using var ctx = dbProvider.Eager();
		
		var existingPermissionIds = await ctx.Permissions.Select(static p => p.Id).AsAsyncEnumerable().ToImmutableSetAsync();
		var missingPermissionIds = GetMissingPermissionsOrdered(Permission.All, existingPermissionIds);
		if (!missingPermissionIds.IsEmpty) {
			Logger.Information("Adding default permissions: {Permissions}", string.Join(", ", missingPermissionIds));
			
			foreach (var permissionId in missingPermissionIds) {
				ctx.Permissions.Add(new PermissionEntity(permissionId));
			}
			
			await ctx.SaveChangesAsync();
		}
	}
	
	public static ImmutableArray<string> GetMissingPermissionsOrdered(IEnumerable<Permission> allPermissions, ImmutableHashSet<string> existingPermissionIds) {
		return allPermissions.Select(static permission => permission.Id).Except(existingPermissionIds).Order().ToImmutableArray();
	}
}
