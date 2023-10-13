using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Data.Web.Users;
using Phantom.Controller.Database.Entities;
using Phantom.Utils.Collections;
using Phantom.Utils.Tasks;

namespace Phantom.Controller.Database.Repositories;

public sealed class RoleRepository {
	private const int MaxRoleNameLength = 40;

	private readonly ILazyDbContext db;

	public RoleRepository(ILazyDbContext db) {
		this.db = db;
	}

	public Task<ImmutableArray<RoleEntity>> GetAll() {
		return db.Ctx.Roles.AsAsyncEnumerable().ToImmutableArrayAsync();
	}
	
	public Task<ImmutableDictionary<Guid, RoleEntity>> GetByGuids(ImmutableHashSet<Guid> guids) {
		return db.Ctx.Roles
		         .Where(role => guids.Contains(role.RoleGuid))
		         .AsAsyncEnumerable()
		         .ToImmutableDictionaryAsync(static role => role.RoleGuid, static role => role);
	}

	public ValueTask<RoleEntity?> GetByGuid(Guid guid) {
		return db.Ctx.Roles.FindAsync(guid);
	}

	public async Task<Result<RoleEntity, AddRoleError>> Create(string name) {
		if (string.IsNullOrWhiteSpace(name)) {
			return AddRoleError.NameIsEmpty;
		}
		else if (name.Length > MaxRoleNameLength) {
			return AddRoleError.NameIsTooLong;
		}

		if (await db.Ctx.Roles.AnyAsync(role => role.Name == name)) {
			return AddRoleError.NameAlreadyExists;
		}

		var role = new RoleEntity(Guid.NewGuid(), name);
		db.Ctx.Roles.Add(role);
		return role;
	}
}
