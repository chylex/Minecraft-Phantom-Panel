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

	private byte[] WriteBytes<TMessage, TReply>(TMessage message) where TMessage : IMessageToServer<TReply> {
		return MessageRegistries.ToServer.Write<TMessage, TReply>(message).ToArray();
	}

	internal async Task Send<TMessage>(TMessage message) where TMessage : IMessageToServer {
		var bytes = WriteBytes<TMessage, NoReply>(message);
		if (bytes.Length > 0) {
			await socket.SendAsync(bytes);
		}
	}

	internal async Task<TReply?> Send<TMessage, TReply>(Func<uint, TMessage> messageFactory, TimeSpan waitForReplyTime, CancellationToken cancellationToken) where TMessage : IMessageToServer<TReply> where TReply : class {
		var sequenceId = replyTracker.RegisterReply();
		var message = messageFactory(sequenceId);
		
		var bytes = WriteBytes<TMessage, TReply>(message);
		if (bytes.Length == 0) {
			replyTracker.ForgetReply(sequenceId);
			return null;
		}

		await socket.SendAsync(bytes);
		return await replyTracker.WaitForReply<TReply>(message.SequenceId, waitForReplyTime, cancellationToken);
	}

	public void Receive(ReplyMessage message) {
		replyTracker.ReceiveReply(message.SequenceId, message.SerializedReply);
	}
}
