using System.Collections.Concurrent;
using Phantom.Common.Data.Web.Users;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Entities;
using Phantom.Controller.Database.Repositories;

namespace Phantom.Controller.Services.Users.Sessions;

sealed class AuthenticatedUserCache {
	private readonly ConcurrentDictionary<Guid, AuthenticatedUserInfo> authenticatedUsersByGuid = new ();

	public bool TryGet(Guid userGuid, out AuthenticatedUserInfo? userInfo) {
		return authenticatedUsersByGuid.TryGetValue(userGuid, out userInfo);
	}

	public async Task<AuthenticatedUserInfo?> Update(UserEntity user, ILazyDbContext db) {
		var userGuid = user.UserGuid;
		var userPermissions = await new PermissionRepository(db).GetAllUserPermissions(user);
		var userInfo = new AuthenticatedUserInfo(userGuid, user.Name, userPermissions);
		return authenticatedUsersByGuid[userGuid] = userInfo;
	}
	
	public void Remove(Guid userGuid) {
		authenticatedUsersByGuid.Remove(userGuid, out _);
	}
}
