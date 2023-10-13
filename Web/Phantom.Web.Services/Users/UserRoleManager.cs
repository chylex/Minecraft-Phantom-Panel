using System.Collections.Immutable;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Web.ToController;
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

	public Task<ChangeUserRolesResult> ChangeUserRoles(Guid loggedInUserGuid, Guid subjectUserGuid, ImmutableHashSet<Guid> addToRoleGuids, ImmutableHashSet<Guid> removeFromRoleGuids, CancellationToken cancellationToken) {
		return controllerConnection.Send<ChangeUserRolesMessage, ChangeUserRolesResult>(new ChangeUserRolesMessage(loggedInUserGuid, subjectUserGuid, addToRoleGuids, removeFromRoleGuids), cancellationToken);
	}
}
