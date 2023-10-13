using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Security.Cryptography;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Controller.Services.Users; 

sealed class UserLoginManager {
	private const int SessionIdBytes = 20;
	private readonly ConcurrentDictionary<string, List<ImmutableArray<byte>>> sessionTokensByUsername = new ();
	
	private readonly UserManager userManager;
	private readonly PermissionManager permissionManager;
	
	public UserLoginManager(UserManager userManager, PermissionManager permissionManager) {
		this.userManager = userManager;
		this.permissionManager = permissionManager;
	}

	public async Task<LogInSuccess?> LogIn(string username, string password) {
		var user = await userManager.GetAuthenticated(username, password);
		if (user == null) {
			return null;
		}

		var token = ImmutableArray.Create(RandomNumberGenerator.GetBytes(SessionIdBytes));
		var sessionTokens = sessionTokensByUsername.GetOrAdd(username, static _ => new List<ImmutableArray<byte>>());
		lock (sessionTokens) {
			sessionTokens.Add(token);
		}
		
		return new LogInSuccess(user.UserGuid, await permissionManager.FetchPermissionsForUserId(user.UserGuid), token);
	}
}
