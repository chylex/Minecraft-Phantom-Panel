namespace Phantom.Utils.Rpc.Runtime;

enum RpcFinalHandshakeResult : byte {
	Error = 0,
	NewSession = 1,
	ReusedSession = 2,
}
