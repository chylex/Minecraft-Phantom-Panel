namespace Phantom.Utils.Rpc.Runtime;

sealed class RpcClientConnectionClosedEventArgs : EventArgs {
	internal uint RoutingId { get; }

	internal RpcClientConnectionClosedEventArgs(uint routingId) {
		RoutingId = routingId;
	}
}
