namespace Phantom.Server.Rpc; 

sealed class RpcClientConnectionClosedEventArgs : EventArgs {
	public uint RoutingId { get; }

	public RpcClientConnectionClosedEventArgs(uint routingId) {
		RoutingId = routingId;
	}
}
