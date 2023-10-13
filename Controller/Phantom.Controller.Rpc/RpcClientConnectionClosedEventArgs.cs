namespace Phantom.Controller.Rpc;

public sealed class RpcClientConnectionClosedEventArgs : EventArgs {
	internal uint RoutingId { get; }

	internal RpcClientConnectionClosedEventArgs(uint routingId) {
		RoutingId = routingId;
	}
}
