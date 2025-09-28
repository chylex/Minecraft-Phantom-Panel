using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime;

namespace Phantom.Utils.Rpc.Frame.Types;

sealed record MessageFrame(uint MessageId, byte MessageTypeCode, ReadOnlyMemory<byte> SerializedMessage) : IFrame {
	public const int MaxMessageBytes = 1024 * 1024 * 8;
	
	public ReadOnlyMemory<byte> FrameType => IFrame.TypeMessage;
	
	public async Task Write(RpcStream stream, CancellationToken cancellationToken) {
		uint serializedMessageLength = (uint) SerializedMessage.Length;
		CheckMessageLength(serializedMessageLength);
		
		await stream.WriteUnsignedInt(MessageId, cancellationToken);
		await stream.WriteByte(MessageTypeCode, cancellationToken);
		await stream.WriteUnsignedInt(serializedMessageLength, cancellationToken);
		await stream.WriteBytes(SerializedMessage, cancellationToken);
	}
	
	public static async Task<MessageFrame> Read(RpcStream stream, CancellationToken cancellationToken) {
		var messageId = await stream.ReadUnsignedInt(cancellationToken);
		var messageTypeCode = await stream.ReadByte(cancellationToken);
		var serializedMessageLength = await stream.ReadUnsignedInt(cancellationToken);
		CheckMessageLength(serializedMessageLength);
		var serializedMessage = await stream.ReadBytes(serializedMessageLength, cancellationToken);
		
		return new MessageFrame(messageId, messageTypeCode, serializedMessage);
	}
	
	private static void CheckMessageLength(uint messageLength) {
		if (messageLength > MaxMessageBytes) {
			throw new MessageErrorException("Message is too large: " + messageLength + " > " + MaxMessageBytes + " bytes", MessageError.MessageTooLarge);
		}
	}
}
