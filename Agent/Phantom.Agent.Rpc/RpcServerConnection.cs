using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Messages;
using Phantom.Common.Messages.BiDirectional;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Agent.Rpc; 

public sealed class RpcServerConnection {
	private readonly ClientSocket socket;
	private readonly MessageReplyTracker replyTracker;

	internal RpcServerConnection(ClientSocket socket, MessageReplyTracker replyTracker) {
		this.socket = socket;
		this.replyTracker = replyTracker;
	}

	internal async Task Send<TMessage>(TMessage message) where TMessage : IMessageToServer {
		var bytes = MessageRegistries.ToServer.Write(message).ToArray();
		if (bytes.Length > 0) {
			await socket.SendAsync(bytes);
		}
	}

	internal async Task<TReply?> Send<TMessage, TReply>(TMessage message, TimeSpan waitForReplyTime, CancellationToken waitForReplyCancellationToken) where TMessage : IMessageToServer<TReply> where TReply : class {
		var sequenceId = replyTracker.RegisterReply();
		
		var bytes = MessageRegistries.ToServer.Write<TMessage, TReply>(sequenceId, message).ToArray();
		if (bytes.Length == 0) {
			replyTracker.ForgetReply(sequenceId);
			return null;
		}

		await socket.SendAsync(bytes);
		return await replyTracker.WaitForReply<TReply>(sequenceId, waitForReplyTime, waitForReplyCancellationToken);
	}

	public void Receive(ReplyMessage message) {
		replyTracker.ReceiveReply(message.SequenceId, message.SerializedReply);
	}
}
