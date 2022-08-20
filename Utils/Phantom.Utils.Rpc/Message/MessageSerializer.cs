using MessagePack;
using MessagePack.Resolvers;

namespace Phantom.Utils.Rpc.Message;

static class MessageSerializer {
	private static readonly MessagePackSerializerOptions SerializerOptions =
		MessagePackSerializerOptions
			.Standard
			.WithResolver(CompositeResolver.Create(NativeGuidResolver.Instance, StandardResolver.Instance))
			.WithCompression(MessagePackCompression.None)
			.WithSecurity(MessagePackSecurity.UntrustedData.WithMaximumObjectGraphDepth(10));

	public static void Serialize<TMessage, TListener>(Stream stream, TMessage message, CancellationToken cancellationToken) where TMessage : IMessage<TListener> {
		MessagePackSerializer.Serialize(stream, message, SerializerOptions, cancellationToken);
	}

	public static Func<ReadOnlyMemory<byte>, CancellationToken, TMessageBase> Deserialize<TMessage, TMessageBase, TListener>() where TMessageBase : IMessage<TListener> where TMessage : TMessageBase {
		return static (memory, cancellationToken) => MessagePackSerializer.Deserialize<TMessage>(memory, SerializerOptions, cancellationToken);
	}

	public static void WriteCode(Stream stream, ushort value) {
		stream.WriteByte((byte) (value & 0xFF));
		stream.WriteByte((byte) ((value >> 8) & 0xFF));
	}

	public static void WriteSequenceId(Stream stream, uint value) {
		stream.WriteByte((byte) (value & 0xFF));
		stream.WriteByte((byte) ((value >> 8) & 0xFF));
		stream.WriteByte((byte) ((value >> 16) & 0xFF));
		stream.WriteByte((byte) ((value >> 24) & 0xFF));
	}

	public static ushort ReadCode(ref ReadOnlyMemory<byte> memory) {
		ushort value = (ushort) (memory.Span[0] | (memory.Span[1] << 8));
		memory = memory[2..];
		return value;
	}

	public static uint ReadSequenceId(ref ReadOnlyMemory<byte> memory) {
		uint value = (uint) (memory.Span[0] | (memory.Span[1] << 8) | (memory.Span[2] << 16) | (memory.Span[3] << 24));
		memory = memory[4..];
		return value;
	}
}
