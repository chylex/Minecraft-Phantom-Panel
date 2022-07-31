using MessagePack;
using MessagePack.Resolvers;

namespace Phantom.Common.Rpc.Message;

static class MessageSerializer {
	private static readonly MessagePackSerializerOptions SerializerOptions =
		MessagePackSerializerOptions
			.Standard
			.WithResolver(CompositeResolver.Create(NativeGuidResolver.Instance, StandardResolver.Instance))
			.WithCompression(MessagePackCompression.None)
			.WithSecurity(MessagePackSecurity.UntrustedData.WithMaximumObjectGraphDepth(10));

	public static void Serialize<TMessage, TListener>(Stream stream, ushort code, TMessage message, CancellationToken cancellationToken) where TMessage : IMessage<TListener> {
		WriteCode(stream, code);
		MessagePackSerializer.Serialize(stream, message, SerializerOptions, cancellationToken);
	}

	public static Func<ReadOnlyMemory<byte>, CancellationToken, TMessageBase> Deserialize<TMessage, TMessageBase, TListener>() where TMessageBase : IMessage<TListener> where TMessage : TMessageBase {
		return static (memory, cancellationToken) => MessagePackSerializer.Deserialize<TMessage>(memory, SerializerOptions, cancellationToken);
	}

	private static void WriteCode(Stream stream, ushort value) {
		stream.WriteByte((byte) (value & 0xFF));
		stream.WriteByte((byte) ((value >> 8) & 0xFF));
	}

	public static ushort ReadCode(ref ReadOnlyMemory<byte> memory) {
		ushort value = (ushort) (memory.Span[0] | (memory.Span[1] << 8));
		memory = memory[2..];
		return value;
	}
}
