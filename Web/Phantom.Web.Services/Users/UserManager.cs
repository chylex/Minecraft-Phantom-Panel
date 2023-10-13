using System.Collections.Immutable;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Web.Services.Rpc;

namespace Phantom.Web.Services.Users;

public sealed class UserManager {
	private readonly ControllerConnection controllerConnection;
	
	public UserManager(ControllerConnection controllerConnection) {
		this.controllerConnection = controllerConnection;
	}

	public Task<ImmutableArray<UserInfo>> GetAll(CancellationToken cancellationToken) {
		return controllerConnection.Send<GetUsersMessage, ImmutableArray<UserInfo>>(new GetUsersMessage(), cancellationToken);
	}

	public Task<CreateUserResult> Create(Guid loggedInUserGuid, string username, string password, CancellationToken cancellationToken) {
		return controllerConnection.Send<CreateUserMessage, CreateUserResult>(new CreateUserMessage(loggedInUserGuid, username, password), cancellationToken);
	}
	
	public Task<DeleteUserResult> DeleteByGuid(Guid loggedInUserGuid, Guid userGuid, CancellationToken cancellationToken) {
		return controllerConnection.Send<DeleteUserMessage, DeleteUserResult>(new DeleteUserMessage(loggedInUserGuid, userGuid), cancellationToken);
	}
}
