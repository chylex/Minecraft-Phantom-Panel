using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Phantom.Common.Data.Web.Users;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Repositories;

namespace Phantom.Controller.Services.Users; 

sealed class UserLoginManager {
	private const int SessionIdBytes = 20;
	private readonly ConcurrentDictionary<Guid, UserSession> sessionsByUserGuid = new ();
	
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

		var permissions = await permissionManager.FetchPermissionsForUserId(user.UserGuid);
		var userInfo = new AuthenticatedUserInfo(user.UserGuid, user.Name, permissions);
		var token = ImmutableArray.Create(RandomNumberGenerator.GetBytes(SessionIdBytes));
		
		sessionsByUserGuid.AddOrUpdate(user.UserGuid, UserSession.Create, UserSession.Add, new NewUserSession(userInfo, token));

		await using (var db = dbProvider.Lazy()) {
			var auditLogWriter = new AuditLogRepository(db).Writer(user.UserGuid);
			auditLogWriter.UserLoggedIn(user);
			
			await db.Ctx.SaveChangesAsync();
		}

		return new LogInSuccess(userInfo, token);
	}

	public async Task LogOut(Guid userGuid, ImmutableArray<byte> token) {
		while (true) {
			if (!sessionsByUserGuid.TryGetValue(userGuid, out var oldSession)) {
				return;
			}

			if (sessionsByUserGuid.TryUpdate(userGuid, oldSession.RemoveToken(token), oldSession)) {
				break;
			}
		}

		await using var db = dbProvider.Lazy();
		
		var auditLogWriter = new AuditLogRepository(db).Writer(userGuid);
		auditLogWriter.UserLoggedOut(userGuid);
			
		await db.Ctx.SaveChangesAsync();
	}

	public AuthenticatedUserInfo? GetAuthenticatedUser(Guid userGuid, ImmutableArray<byte> token) {
		return sessionsByUserGuid.TryGetValue(userGuid, out var session) && session.Tokens.Contains(token, TokenEqualityComparer.Instance) ? session.UserInfo : null;
	}

	private readonly record struct NewUserSession(AuthenticatedUserInfo UserInfo, ImmutableArray<byte> Token);
	
	private sealed record UserSession(AuthenticatedUserInfo UserInfo, ImmutableList<ImmutableArray<byte>> Tokens) {
		public static UserSession Create(Guid userGuid, NewUserSession newSession) {
			return new UserSession(newSession.UserInfo, ImmutableList.Create(newSession.Token));
		}
		
		public static UserSession Add(Guid userGuid, UserSession oldSession, NewUserSession newSession) {
			return new UserSession(newSession.UserInfo, oldSession.Tokens.Add(newSession.Token));
		}

		public UserSession RemoveToken(ImmutableArray<byte> token) {
			return this with { Tokens = Tokens.Remove(token, TokenEqualityComparer.Instance) };
		}

		public bool Equals(UserSession? other) {
			return ReferenceEquals(this, other);
		}

		public override int GetHashCode() {
			return RuntimeHelpers.GetHashCode(this);
		}
	}

	private sealed class TokenEqualityComparer : IEqualityComparer<ImmutableArray<byte>> {
		public static TokenEqualityComparer Instance { get; } = new ();
		
		private TokenEqualityComparer() {}

		public bool Equals(ImmutableArray<byte> x, ImmutableArray<byte> y) {
			return x.SequenceEqual(y);
		}

		public int GetHashCode(ImmutableArray<byte> obj) {
			throw new NotImplementedException();
		}
	}
}
