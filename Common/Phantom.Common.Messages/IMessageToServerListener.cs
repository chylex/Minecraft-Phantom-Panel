using Phantom.Common.Messages.ToServer;

namespace Phantom.Common.Messages; 

public interface IMessageToServerListener {
	bool IsDisposed { get; }
	Task HandleRegisterAgent(RegisterAgentMessage message);
	Task HandleUnregisterAgent(UnregisterAgentMessage message);
	Task HandleInstanceOutputLine(InstanceOutputLineMessage message);
	Task HandleSimpleReply(SimpleReplyMessage message);
}
