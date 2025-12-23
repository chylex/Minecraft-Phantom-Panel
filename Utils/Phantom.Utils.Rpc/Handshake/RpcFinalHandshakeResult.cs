namespace Phantom.Utils.Rpc.Handshake;

enum RpcFinalHandshakeResult : byte {
	Error = 0,
	NewSession = 1,
	ReusedSession = 2,
}
