using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Web.Services.Authentication;
using Phantom.Web.Services.Rpc;

namespace Phantom.Web.Services.Users;

public sealed class UserRoleManager {
	private readonly ControllerConnection controllerConnection;
	
	public UserRoleManager(ControllerConnection controllerConnection) {
		this.controllerConnection = controllerConnection;
	}

	public Task<ImmutableDictionary<Guid, ImmutableArray<Guid>>> GetUserRoles(ImmutableHashSet<Guid> userGuids, CancellationToken cancellationToken) {
		return controllerConnection.Send<GetUserRolesMessage, ImmutableDictionary<Guid, ImmutableArray<Guid>>>(new GetUserRolesMessage(userGuids), cancellationToken);
	}
	
	public async Task<ImmutableArray<Guid>> GetUserRoles(Guid userGuid, CancellationToken cancellationToken) {
		return (await GetUserRoles(ImmutableHashSet.Create(userGuid), cancellationToken)).GetValueOrDefault(userGuid, ImmutableArray<Guid>.Empty);
	}

	public async Task<Result<ChangeUserRolesResult, UserActionFailure>> ChangeUserRoles(AuthenticatedUser? authenticatedUser, Guid subjectUserGuid, ImmutableHashSet<Guid> addToRoleGuids, ImmutableHashSet<Guid> removeFromRoleGuids, CancellationToken cancellationToken) {
		if (authenticatedUser != null && authenticatedUser.Info.CheckPermission(Permission.EditUsers)) {
			return await controllerConnection.Send<ChangeUserRolesMessage, Result<ChangeUserRolesResult, UserActionFailure>>(new ChangeUserRolesMessage(authenticatedUser.Token, subjectUserGuid, addToRoleGuids, removeFromRoleGuids), cancellationToken);
		}
		else {
			return UserActionFailure.NotAuthorized;
		}
	}
}
