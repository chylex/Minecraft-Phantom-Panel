using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Phantom.Web.Services.Authentication;

public sealed class UserSessionManager {
	private readonly ConcurrentDictionary<Guid, UserSessions> userSessions = new ();

	internal void Add(UserInfo user, ImmutableArray<byte> token) {
		userSessions.AddOrUpdate(
			user.UserGuid,
			static (_, u) => new UserSessions(u),
			static (_, sessions, u) => sessions.WithUserInfo(u),
			user
		).AddToken(token);
	}

	internal UserInfo? Find(Guid userGuid) {
		return userSessions.TryGetValue(userGuid, out var sessions) ? sessions.UserInfo : null;
	}
	
	internal UserInfo? FindWithToken(Guid userGuid, ImmutableArray<byte> token) {
		return userSessions.TryGetValue(userGuid, out var sessions) && sessions.HasToken(token) ? sessions.UserInfo : null;
	}

	internal bool Remove(Guid userGuid, ImmutableArray<byte> token) {
		if (userSessions.TryGetValue(userGuid, out var sessions)) {
			sessions.RemoveToken(token);
			return true;
		}
		else {
			return false;
		}
	}
}
