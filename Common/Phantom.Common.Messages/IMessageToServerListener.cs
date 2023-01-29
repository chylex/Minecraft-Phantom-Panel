using Phantom.Common.Messages.BiDirectional;
using Phantom.Common.Messages.ToServer;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages; 

public interface IMessageToServerListener {
	bool IsDisposed { get; }
	Task<NoReply> HandleRegisterAgent(RegisterAgentMessage message);
	Task<NoReply> HandleUnregisterAgent(UnregisterAgentMessage message);
	Task<NoReply> HandleAgentIsAlive(AgentIsAliveMessage message);
	Task<NoReply> HandleAdvertiseJavaRuntimes(AdvertiseJavaRuntimesMessage message);
	Task<NoReply> HandleReportAgentStatus(ReportAgentStatusMessage message);
	Task<NoReply> HandleReportInstanceStatus(ReportInstanceStatusMessage message);
	Task<NoReply> HandleInstanceOutput(InstanceOutputMessage message);
	Task<NoReply> HandleReply(ReplyMessage message);
}
