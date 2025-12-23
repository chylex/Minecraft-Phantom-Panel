namespace Phantom.Utils.Rpc.Runtime.Server;

public interface IRpcServerClientAuthProvider {
	Task<AuthSecret?> GetAuthSecret(Guid clientGuid);
}
