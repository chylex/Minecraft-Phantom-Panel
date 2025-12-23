using Phantom.Utils.Rpc;
using Phantom.Utils.Rpc.Runtime.Server;

namespace Phantom.Controller.Services.Rpc;

public sealed class WebClientAuthProvider(AuthToken webAuthToken) : IRpcServerClientAuthProvider {
	public Task<AuthSecret?> GetAuthSecret(Guid clientGuid) {
		return Task.FromResult(clientGuid == webAuthToken.Guid ? webAuthToken.Secret : null);
	}
}
