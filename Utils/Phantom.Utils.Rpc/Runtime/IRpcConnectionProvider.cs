namespace Phantom.Utils.Rpc.Runtime;

interface IRpcConnectionProvider {
	Task<RpcStream> GetStream(CancellationToken cancellationToken);
}
