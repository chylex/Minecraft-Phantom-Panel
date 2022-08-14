using Phantom.Server.Rpc;

namespace Phantom.Server.Services.Agents; 

public sealed class AgentInfo {
	private readonly RpcClientConnection connection;
	private readonly int version;
	
	public Guid Guid { get; }
	public string Name { get; }

	internal AgentInfo(Guid guid, RpcClientConnection connection, int version, string name) {
		this.connection = connection;
		this.version = version;
		
		this.Guid = guid;
		this.Name = name;
	}
}
