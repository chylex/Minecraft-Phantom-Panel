using Phantom.Common.Messages.ToServer;

namespace Phantom.Common.Messages; 

public interface IMessageToServerListener {
	bool IsDisposed { get; }
	Task HandleRegisterAgent(RegisterAgentMessage message);
	Task HandleUnregisterAgent(UnregisterAgentMessage message);
	Task HandleAgentIsAlive(AgentIsAliveMessage message);
	Task HandleInstanceOutput(InstanceOutputMessage message);
	Task HandleAdvertiseJavaRuntimes(AdvertiseJavaRuntimesMessage message);
	Task HandleSimpleReply(SimpleReplyMessage message);
}
