using System.Buffers.Binary;
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
		Span<byte> buffer = stackalloc byte[2];
		BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
		stream.Write(buffer);
	}

	public static ushort ReadCode(ref ReadOnlyMemory<byte> memory) {
		ushort value = BinaryPrimitives.ReadUInt16LittleEndian(memory.Span);
		memory = memory[2..];
		return value;
	}
}
