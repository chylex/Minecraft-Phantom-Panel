﻿using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.Agent.BiDirectional;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Agent;

public static class AgentMessageRegistries {
	public static MessageRegistry<IMessageToAgent> ToAgent { get; } = new (PhantomLogger.Create("MessageRegistry", nameof(ToAgent)));
	public static MessageRegistry<IMessageToController> ToController { get; } = new (PhantomLogger.Create("MessageRegistry", nameof(ToController)));
	
	public static IMessageDefinitions<IMessageToAgent, IMessageToController, ReplyMessage> Definitions { get; } = new MessageDefinitions();

	static AgentMessageRegistries() {
		ToAgent.Add<RegisterAgentSuccessMessage>(0);
		ToAgent.Add<RegisterAgentFailureMessage>(1);
		ToAgent.Add<ConfigureInstanceMessage, InstanceActionResult<ConfigureInstanceResult>>(2);
		ToAgent.Add<LaunchInstanceMessage, InstanceActionResult<LaunchInstanceResult>>(3);
		ToAgent.Add<StopInstanceMessage, InstanceActionResult<StopInstanceResult>>(4);
		ToAgent.Add<SendCommandToInstanceMessage, InstanceActionResult<SendCommandToInstanceResult>>(5);
		ToAgent.Add<ReplyMessage>(127);
		
		ToController.Add<RegisterAgentMessage>(0);
		ToController.Add<UnregisterAgentMessage>(1);
		ToController.Add<AgentIsAliveMessage>(2);
		ToController.Add<AdvertiseJavaRuntimesMessage>(3);
		ToController.Add<ReportInstanceStatusMessage>(4);
		ToController.Add<InstanceOutputMessage>(5);
		ToController.Add<ReportAgentStatusMessage>(6);
		ToController.Add<ReportInstanceEventMessage>(7);
		ToController.Add<ReplyMessage>(127);
	}

	private sealed class MessageDefinitions : IMessageDefinitions<IMessageToAgent, IMessageToController, ReplyMessage> {
		public MessageRegistry<IMessageToAgent> ToClient => ToAgent;
		public MessageRegistry<IMessageToController> ToServer => ToController;

		public ReplyMessage CreateReplyMessage(uint sequenceId, byte[] serializedReply) {
			return new ReplyMessage(sequenceId, serializedReply);
		}
	}
}
