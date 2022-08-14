using Phantom.Common.Data;
using Phantom.Server.Rpc;

namespace Phantom.Server.Services.Agents; 

internal sealed class AgentConnection {
	private readonly RpcClientConnection connection;

	public AgentInfo Info { get; }

	internal AgentConnection(RpcClientConnection connection, AgentInfo info) {
		this.connection = connection;
		this.Info = info;
	}
}
