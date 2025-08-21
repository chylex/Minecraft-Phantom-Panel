using Phantom.Utils.Rpc.Runtime;

namespace Phantom.Utils.Rpc.Message;

sealed class ReplySender<TMessageBase, TReplyMessage> : IReplySender where TReplyMessage : TMessageBase {
	private readonly RpcConnection<TMessageBase> connection;
	private readonly IReplyMessageFactory<TReplyMessage> replyMessageFactory;
	
	public ReplySender(RpcConnection<TMessageBase> connection, IReplyMessageFactory<TReplyMessage> replyMessageFactory) {
		this.connection = connection;
		this.replyMessageFactory = replyMessageFactory;
	}
	
	public Task SendReply(uint sequenceId, byte[] serializedReply) {
		return connection.Send(replyMessageFactory.CreateReplyMessage(sequenceId, serializedReply));
	}
}
