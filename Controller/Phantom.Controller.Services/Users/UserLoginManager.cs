using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Security.Cryptography;
using Phantom.Common.Data.Web.Users;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Repositories;

namespace Phantom.Controller.Services.Users; 

sealed class UserLoginManager {
	private const int SessionIdBytes = 20;
	private readonly ConcurrentDictionary<Guid, List<ImmutableArray<byte>>> sessionTokensByUserGuid = new ();
	
	private readonly UserManager userManager;
	private readonly PermissionManager permissionManager;
	private readonly IDbContextProvider dbProvider;
	
	public UserLoginManager(UserManager userManager, PermissionManager permissionManager, IDbContextProvider dbProvider) {
		this.userManager = userManager;
		this.permissionManager = permissionManager;
		this.dbProvider = dbProvider;
	}

	public async Task<LogInSuccess?> LogIn(string username, string password) {
		var user = await userManager.GetAuthenticated(username, password);
		if (user == null) {
			return null;
		}

		var token = ImmutableArray.Create(RandomNumberGenerator.GetBytes(SessionIdBytes));
		var sessionTokens = sessionTokensByUserGuid.GetOrAdd(user.UserGuid, static _ => new List<ImmutableArray<byte>>());
		lock (sessionTokens) {
			sessionTokens.Add(token);
		}

		await using (var db = dbProvider.Lazy()) {
			var auditLogWriter = new AuditLogRepository(db).Writer(user.UserGuid);
			auditLogWriter.UserLoggedIn(user);
			
			await db.Ctx.SaveChangesAsync();
		}

		return new LogInSuccess(user.UserGuid, await permissionManager.FetchPermissionsForUserId(user.UserGuid), token);
	}

	public async Task LogOut(Guid userGuid, ImmutableArray<byte> sessionToken) {
		if (!sessionTokensByUserGuid.TryGetValue(userGuid, out var sessionTokens)) {
			return;
		}

		lock (sessionTokens) {
			if (sessionTokens.RemoveAll(token => token.SequenceEqual(sessionToken)) == 0) {
				return;
			}
		}

		await using var db = dbProvider.Lazy();
		
		var auditLogWriter = new AuditLogRepository(db).Writer(userGuid);
		auditLogWriter.UserLoggedOut(userGuid);
			
		await db.Ctx.SaveChangesAsync();
	}
}
