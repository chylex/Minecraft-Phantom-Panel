using System.Collections.Immutable;
using System.Security.Cryptography;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.Users;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Repositories;

namespace Phantom.Controller.Services.Users.Sessions;

sealed class UserLoginManager {
	private const int SessionIdBytes = 20;
	
	private readonly AuthenticatedUserCache authenticatedUserCache;
	private readonly IDbContextProvider dbProvider;
	
	private readonly UserSessionBucket[] sessionBuckets = new UserSessionBucket[256];
	
	public UserLoginManager(AuthenticatedUserCache authenticatedUserCache, IDbContextProvider dbProvider) {
		this.authenticatedUserCache = authenticatedUserCache;
		this.dbProvider = dbProvider;
		
		for (int i = 0; i < sessionBuckets.GetLength(0); i++) {
			sessionBuckets[i] = new UserSessionBucket();
		}
	}
	
	private UserSessionBucket GetSessionBucket(ImmutableArray<byte> token) {
		return sessionBuckets[token[0]];
	}
	
	public async Task<Optional<LogInSuccess>> LogIn(string username, string password) {
		Guid userGuid;
		AuthenticatedUserInfo? authenticatedUserInfo;
		
		await using (var db = dbProvider.Lazy()) {
			var userRepository = new UserRepository(db);
			
			var user = await userRepository.GetByName(username);
			if (user == null || !UserPasswords.Verify(password, user.PasswordHash)) {
				return default;
			}
			
			authenticatedUserInfo = await authenticatedUserCache.Update(user, db);
			if (authenticatedUserInfo == null) {
				return default;
			}
			
			userGuid = user.UserGuid;
			
			var auditLogWriter = new AuditLogRepository(db).Writer(userGuid);
			auditLogWriter.UserLoggedIn(user);
			
			await db.Ctx.SaveChangesAsync();
		}
		
		var authToken = ImmutableArray.Create(RandomNumberGenerator.GetBytes(SessionIdBytes));
		GetSessionBucket(authToken).Add(userGuid, authToken);
		
		return new LogInSuccess(authenticatedUserInfo, authToken);
	}
	
	public async Task LogOut(Guid userGuid, ImmutableArray<byte> authToken) {
		if (!GetSessionBucket(authToken).Remove(userGuid, authToken)) {
			return;
		}
		
		await using var db = dbProvider.Lazy();
		
		var auditLogWriter = new AuditLogRepository(db).Writer(userGuid);
		auditLogWriter.UserLoggedOut(userGuid);
		
		await db.Ctx.SaveChangesAsync();
	}
	
	public LoggedInUser GetLoggedInUser(ImmutableArray<byte> authToken) {
		var userGuid = GetSessionBucket(authToken).FindUserGuid(authToken);
		return userGuid != null && authenticatedUserCache.TryGet(userGuid.Value, out var userInfo) ? new LoggedInUser(userInfo) : default;
	}
	
	public AuthenticatedUserInfo? GetAuthenticatedUser(Guid userGuid, ImmutableArray<byte> authToken) {
		return authenticatedUserCache.TryGet(userGuid, out var userInfo) && GetSessionBucket(authToken).Contains(userGuid, authToken) ? userInfo : null;
	}
	
	private sealed class UserSessionBucket {
		private ImmutableList<UserSession> sessions = ImmutableList<UserSession>.Empty;
		
		public void Add(Guid userGuid, ImmutableArray<byte> authToken) {
			lock (this) {
				var session = new UserSession(userGuid, authToken);
				if (!sessions.Contains(session)) {
					sessions = sessions.Add(session);
				}
			}
		}
		
		public bool Contains(Guid userGuid, ImmutableArray<byte> authToken) {
			lock (this) {
				return sessions.Contains(new UserSession(userGuid, authToken));
			}
		}
		
		public Guid? FindUserGuid(ImmutableArray<byte> authToken) {
			lock (this) {
				return sessions.Find(session => session.AuthTokenEquals(authToken))?.UserGuid;
			}
		}
		
		public bool Remove(Guid userGuid, ImmutableArray<byte> authToken) {
			lock (this) {
				int index = sessions.IndexOf(new UserSession(userGuid, authToken));
				if (index == -1) {
					return false;
				}
				
				sessions = sessions.RemoveAt(index);
				return true;
			}
		}
	}
	
	private sealed record UserSession(Guid UserGuid, ImmutableArray<byte> AuthToken) {
		public bool AuthTokenEquals(ImmutableArray<byte> other) {
			return CryptographicOperations.FixedTimeEquals(AuthToken.AsSpan(), other.AsSpan());
		}
		
		public bool Equals(UserSession? other) {
			if (other is null) {
				return false;
			}
			
			return UserGuid.Equals(other.UserGuid) && AuthTokenEquals(other.AuthToken);
		}
		
		public override int GetHashCode() {
			throw new NotImplementedException();
		}
	}
}
