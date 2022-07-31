using Phantom.Server.Rpc;

namespace Phantom.Server.Services.Agents; 

public sealed class AgentInfo {
	private readonly RpcClientConnection connection;
	private readonly int version;

	internal AgentInfo(RpcClientConnection connection, int version) {
		this.connection = connection;
		this.version = version;
	}
}
