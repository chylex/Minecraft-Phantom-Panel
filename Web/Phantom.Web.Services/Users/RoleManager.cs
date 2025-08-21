using System.Collections.Immutable;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Web.Services.Rpc;

namespace Phantom.Web.Services.Users;

public sealed class RoleManager {
	private readonly ControllerConnection controllerConnection;
	
	public RoleManager(ControllerConnection controllerConnection) {
		this.controllerConnection = controllerConnection;
	}
	
	public Task<ImmutableArray<RoleInfo>> GetAll(CancellationToken cancellationToken) {
		return controllerConnection.Send<GetRolesMessage, ImmutableArray<RoleInfo>>(new GetRolesMessage(), cancellationToken);
	}
}
