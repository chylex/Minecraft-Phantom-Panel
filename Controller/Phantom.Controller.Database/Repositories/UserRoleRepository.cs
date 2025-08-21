using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Phantom.Controller.Database.Entities;
using Phantom.Utils.Collections;

namespace Phantom.Controller.Database.Repositories;

public sealed class UserRoleRepository {
	private readonly ILazyDbContext db;
	
	public UserRoleRepository(ILazyDbContext db) {
		this.db = db;
	}
	
	public async Task<ImmutableDictionary<Guid, ImmutableArray<Guid>>> GetRoleGuidsByUserGuid(ImmutableHashSet<Guid> userGuids) {
		var result = await db.Ctx.UserRoles
		                     .Where(ur => userGuids.Contains(ur.UserGuid))
		                     .GroupBy(static ur => ur.UserGuid, static ur => ur.RoleGuid)
		                     .AsAsyncEnumerable()
		                     .ToImmutableDictionaryAsync(static group => group.Key, static group => group.ToImmutableArray());
		
		foreach (var userGuid in userGuids) {
			if (!result.ContainsKey(userGuid)) {
				result = result.Add(userGuid, ImmutableArray<Guid>.Empty);
			}
		}
		
		return result;
	}
	
	public Task<Dictionary<Guid, ImmutableArray<RoleEntity>>> GetAllByUserGuid() {
		return db.Ctx.UserRoles
		         .Include(static ur => ur.Role)
		         .GroupBy(static ur => ur.UserGuid, static ur => ur.Role)
		         .ToDictionaryAsync(static group => group.Key, static group => group.ToImmutableArray());
	}
	
	public Task<ImmutableArray<RoleEntity>> GetUserRoles(UserEntity user) {
		return db.Ctx.UserRoles
		         .Include(static ur => ur.Role)
		         .Where(ur => ur.UserGuid == user.UserGuid)
		         .Select(static ur => ur.Role)
		         .AsAsyncEnumerable()
		         .ToImmutableArrayAsync();
	}
	
	public Task<ImmutableHashSet<Guid>> GetUserRoleGuids(UserEntity user) {
		return db.Ctx.UserRoles
		         .Where(ur => ur.UserGuid == user.UserGuid)
		         .Select(static ur => ur.RoleGuid)
		         .AsAsyncEnumerable()
		         .ToImmutableSetAsync();
	}
	
	public async Task Add(UserEntity user, RoleEntity role) {
		var userRole = await db.Ctx.UserRoles.FindAsync(user.UserGuid, role.RoleGuid);
		if (userRole == null) {
			db.Ctx.UserRoles.Add(new UserRoleEntity(user.UserGuid, role.RoleGuid));
		}
	}
	
	public async Task<UserRoleEntity?> Remove(UserEntity user, RoleEntity role) {
		var userRole = await db.Ctx.UserRoles.FindAsync(user.UserGuid, role.RoleGuid);
		if (userRole == null) {
			return null;
		}
		else {
			db.Ctx.UserRoles.Remove(userRole);
			return userRole;
		}
	}
}
