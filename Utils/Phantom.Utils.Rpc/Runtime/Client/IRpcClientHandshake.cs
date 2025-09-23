namespace Phantom.Utils.Rpc.Runtime.Client;

public interface IRpcClientHandshake {
	Task Perform(RpcStream stream, CancellationToken cancellationToken);
	
	sealed record NoOp : IRpcClientHandshake {
		public Task Perform(RpcStream stream, CancellationToken cancellationToken) {
			return Task.CompletedTask;
		}
	}
}
