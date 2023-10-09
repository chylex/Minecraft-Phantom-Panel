using Phantom.Common.Messages.Agent.BiDirectional;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Agent;

public interface IMessageToControllerListener {
	Task<NoReply> HandleRegisterAgent(RegisterAgentMessage message);
	Task<NoReply> HandleUnregisterAgent(UnregisterAgentMessage message);
	Task<NoReply> HandleAgentIsAlive(AgentIsAliveMessage message);
	Task<NoReply> HandleAdvertiseJavaRuntimes(AdvertiseJavaRuntimesMessage message);
	Task<NoReply> HandleReportAgentStatus(ReportAgentStatusMessage message);
	Task<NoReply> HandleReportInstanceStatus(ReportInstanceStatusMessage message);
	Task<NoReply> HandleReportInstanceEvent(ReportInstanceEventMessage message);
	Task<NoReply> HandleInstanceOutput(InstanceOutputMessage message);
	Task<NoReply> HandleReply(ReplyMessage message);
}
