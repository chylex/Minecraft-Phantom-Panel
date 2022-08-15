using Phantom.Common.Rpc.Messages.ToServer;

namespace Phantom.Common.Rpc.Messages; 

public interface IMessageToServerListener {
	bool IsDisposed { get; }
	Task HandleRegisterAgent(RegisterAgentMessage message);
	Task HandleUnregisterAgent(UnregisterAgentMessage message);
}
