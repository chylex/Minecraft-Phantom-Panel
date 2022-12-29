using System.Buffers;
using System.Buffers.Binary;
using MemoryPack;

namespace Phantom.Utils.Rpc.Message;

static class MessageSerializer {
	private static readonly MemoryPackSerializerOptions SerializerOptions = MemoryPackSerializerOptions.Utf8;

	public static void Serialize<TMessage, TListener>(IBufferWriter<byte> destination, TMessage message) where TMessage : IMessage<TListener> {
		MemoryPackSerializer.Serialize(typeof(TMessage), destination, message, SerializerOptions);
	}

	public static Func<ReadOnlyMemory<byte>, TMessageBase> Deserialize<TMessage, TMessageBase, TListener>() where TMessageBase : IMessage<TListener> where TMessage : TMessageBase {
		return static memory => MemoryPackSerializer.Deserialize<TMessage>(memory.Span) ?? throw new NullReferenceException();
	}

	public static void WriteCode(IBufferWriter<byte> destination, ushort value) {
		Span<byte> buffer = stackalloc byte[2];
		BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
		destination.Write(buffer);
	}

	public static ushort ReadCode(ref ReadOnlyMemory<byte> memory) {
		ushort value = BinaryPrimitives.ReadUInt16LittleEndian(memory.Span);
		memory = memory[2..];
		return value;
	}
}
