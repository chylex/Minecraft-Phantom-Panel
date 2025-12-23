namespace Phantom.Utils.Rpc.Handshake;

enum RpcAuthResult : byte {
	UnknownClient = 0,
	InvalidSecret = 1,
	Success = 255,
}
