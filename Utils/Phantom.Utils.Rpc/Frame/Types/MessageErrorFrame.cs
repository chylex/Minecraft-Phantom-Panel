using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime;

namespace Phantom.Utils.Rpc.Frame.Types;

sealed record MessageErrorFrame(uint ReplyingToMessageId, MessageError Error) : IFrame {
	public ReadOnlyMemory<byte> FrameType => IFrame.TypeError;
	
	public async Task Write(RpcStream stream, CancellationToken cancellationToken) {
		await stream.WriteUnsignedInt(ReplyingToMessageId, cancellationToken);
		await stream.WriteByte((byte) Error, cancellationToken);
	}
	
	public static async Task<MessageErrorFrame> Read(RpcStream stream, CancellationToken cancellationToken) {
		var replyingToMessageId = await stream.ReadUnsignedInt(cancellationToken);
		var messageError = (MessageError) await stream.ReadByte(cancellationToken);
		return new MessageErrorFrame(replyingToMessageId, messageError);
	}
}
