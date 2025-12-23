namespace Phantom.Utils.Rpc.Runtime.Server;

public interface IRpcServerClientHandshake {
	Task Perform(bool isNewSession, RpcStream stream, Guid clientGuid, CancellationToken cancellationToken);
	
	sealed record NoOp : IRpcServerClientHandshake {
		public Task Perform(bool isNewSession, RpcStream stream, Guid clientGuid, CancellationToken cancellationToken) {
			return Task.CompletedTask;
		}
	}
}
