using BinaryPack.Attributes;
using Phantom.Common.Rpc.Message;

namespace Phantom.Common.Rpc.Messages.ToServer; 

[BinarySerialization]
public sealed class AgentAuthenticationMessage : IMessageToServer {
	public Guid AgentGuid { get; set; }
	public int AgentVersion { get; set; }
	public string AuthToken { get; set; }

	public void Accept(IMessageToServerListener listener) {
		listener.HandleAgentAuthentication(this);
	}
}
