using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Logging;
using Phantom.Server.Database;
using Phantom.Server.Database.Entities;
using Phantom.Utils.Collections;
using Phantom.Utils.Tasks;
using ILogger = Serilog.ILogger;

namespace Phantom.Server.Services.Users;

public sealed class RoleManager {
	private static readonly ILogger Logger = PhantomLogger.Create<RoleManager>();

	private const int MaxRoleNameLength = 40;

	private readonly ApplicationDbContext db;

	public RoleManager(ApplicationDbContext db) {
		this.db = db;
	}

	public Task<List<RoleEntity>> GetAll() {
		return db.Roles.ToListAsync();
	}

	public Task<ImmutableHashSet<string>> GetAllNames() {
		return db.Roles.Select(static role => role.Name).AsAsyncEnumerable().ToImmutableSetAsync();
	}

	public ValueTask<RoleEntity?> GetByGuid(Guid guid) {
		return db.Roles.FindAsync(guid);
	}

	public async Task<Result<RoleEntity, AddRoleError>> Create(Guid guid, string name) {
		if (string.IsNullOrWhiteSpace(name)) {
			return Result.Fail<RoleEntity, AddRoleError>(AddRoleError.NameIsEmpty);
		}
		else if (name.Length > MaxRoleNameLength) {
			return Result.Fail<RoleEntity, AddRoleError>(AddRoleError.NameIsTooLong);
		}

		try {
			if (await db.Roles.AnyAsync(role => role.Name == name)) {
				return Result.Fail<RoleEntity, AddRoleError>(AddRoleError.NameAlreadyExists);
			}

			var role = new RoleEntity(guid, name);

			db.Roles.Add(role);
			await db.SaveChangesAsync();

			Logger.Information("Created role \"{Name}\" (GUID {Guid}).", name, guid);
			return Result.Ok<RoleEntity, AddRoleError>(role);
		} catch (Exception e) {
			Logger.Error(e, "Could not create role \"{Name}\" (GUID {Guid}).", name, guid);
			return Result.Fail<RoleEntity, AddRoleError>(AddRoleError.UnknownError);
		}
	}
}
