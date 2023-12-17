using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Agent;

public interface IMessageToAgent<TReply> : IMessage<IMessageToAgentListener, TReply> {
	MessageQueueKey IMessage<IMessageToAgentListener, TReply>.QueueKey => IMessageToAgent.DefaultQueueKey;
}

public interface IMessageToAgent : IMessageToAgent<NoReply> {
	internal static readonly MessageQueueKey DefaultQueueKey = new ("Agent.Default");
	MessageQueueKey IMessage<IMessageToAgentListener, NoReply>.QueueKey => DefaultQueueKey;
}
