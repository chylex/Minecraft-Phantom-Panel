using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime;

namespace Phantom.Utils.Rpc.Frame.Types;

sealed record MessageReplyFrame(uint ReplyingToMessageId, ReadOnlyMemory<byte> SerializedReply) : IFrame {
	public const int MaxReplyBytes = 1024 * 1024 * 32;
	
	public ReadOnlyMemory<byte> FrameType => IFrame.TypeReply;
	
	public async Task Write(RpcStream stream, CancellationToken cancellationToken) {
		uint serializedReplyLength = (uint) SerializedReply.Length;
		CheckReplyLength(serializedReplyLength);
		
		await stream.WriteUnsignedInt(ReplyingToMessageId, cancellationToken);
		await stream.WriteUnsignedInt(serializedReplyLength, cancellationToken);
		await stream.WriteBytes(SerializedReply, cancellationToken);
	}
	
	public static async Task<MessageReplyFrame> Read(RpcStream stream, CancellationToken cancellationToken) {
		var replyingToMessageId = await stream.ReadUnsignedInt(cancellationToken);
		var serializedReplyLength = await stream.ReadUnsignedInt(cancellationToken);
		CheckReplyLength(serializedReplyLength);
		var serializedReply = await stream.ReadBytes(serializedReplyLength, cancellationToken);
		
		return new MessageReplyFrame(replyingToMessageId, serializedReply);
	}
	
	private static void CheckReplyLength(uint replyLength) {
		if (replyLength > MaxReplyBytes) {
			throw new MessageErrorException("Reply is too large: " + replyLength + " > " + MaxReplyBytes + " bytes", MessageError.ReplyTooLarge);
		}
	}
}
