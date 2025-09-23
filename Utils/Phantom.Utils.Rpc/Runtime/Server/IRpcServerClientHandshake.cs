using Phantom.Utils.Monads;

namespace Phantom.Utils.Rpc.Runtime.Server;

public interface IRpcServerClientHandshake<T> {
	Task<Either<T, Exception>> Perform(bool isNewSession, RpcStream stream, CancellationToken cancellationToken);
}

public static class RpcServerClientHandshake {
	public readonly record struct NoValue;
	
	public sealed record NoOp : IRpcServerClientHandshake<NoValue> {
		public Task<Either<NoValue, Exception>> Perform(bool isNewSession, RpcStream stream, CancellationToken cancellationToken) {
			return Task.FromResult<Either<NoValue, Exception>>(Either.Left(new NoValue()));
		}
	}
}
