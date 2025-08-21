using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Web.Services.Authentication;
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
	
	public async Task<Result<CreateUserResult, UserActionFailure>> Create(AuthenticatedUser? authenticatedUser, string username, string password, CancellationToken cancellationToken) {
		if (authenticatedUser != null && authenticatedUser.Info.CheckPermission(Permission.EditUsers)) {
			return await controllerConnection.Send<CreateUserMessage, Result<CreateUserResult, UserActionFailure>>(new CreateUserMessage(authenticatedUser.Token, username, password), cancellationToken);
		}
		else {
			return UserActionFailure.NotAuthorized;
		}
	}
	
	public async Task<Result<DeleteUserResult, UserActionFailure>> DeleteByGuid(AuthenticatedUser? authenticatedUser, Guid userGuid, CancellationToken cancellationToken) {
		if (authenticatedUser != null && authenticatedUser.Info.CheckPermission(Permission.EditUsers)) {
			return await controllerConnection.Send<DeleteUserMessage, Result<DeleteUserResult, UserActionFailure>>(new DeleteUserMessage(authenticatedUser.Token, userGuid), cancellationToken);
		}
		else {
			return UserActionFailure.NotAuthorized;
		}
	}
}
