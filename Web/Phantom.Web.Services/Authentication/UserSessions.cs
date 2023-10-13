using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Phantom.Web.Services.Authentication; 

sealed class UserSessions {
	public UserInfo UserInfo { get; }
	
	private readonly List<ImmutableArray<byte>> tokens = new ();
	
	public UserSessions(UserInfo userInfo) {
		UserInfo = userInfo;
	}

	private UserSessions(UserInfo userInfo, List<ImmutableArray<byte>> tokens) : this(userInfo) {
		this.tokens.AddRange(tokens);
	}

	public UserSessions WithUserInfo(UserInfo user) {
		List<ImmutableArray<byte>> tokensCopy;
		lock (tokens) {
			tokensCopy = new List<ImmutableArray<byte>>(tokens);
		}
		
		return new UserSessions(user, tokensCopy);
	}

	public void AddToken(ImmutableArray<byte> token) {
		lock (tokens) {
			if (!HasToken(token)) {
				tokens.Add(token);
			}
		}
	}

	public bool HasToken(ImmutableArray<byte> token) {
		return FindTokenIndex(token) != -1;
	}

	private int FindTokenIndex(ImmutableArray<byte> token) {
		lock (tokens) {
			return tokens.FindIndex(t => CryptographicOperations.FixedTimeEquals(t.AsSpan(), token.AsSpan()));
		}
	}

	public void RemoveToken(ImmutableArray<byte> token) {
		lock (tokens) {
			int index = FindTokenIndex(token);
			if (index != -1) {
				tokens.RemoveAt(index);
			}
		}
	}
}
