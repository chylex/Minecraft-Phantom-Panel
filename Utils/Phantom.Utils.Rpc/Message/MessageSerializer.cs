using System.Buffers;
using System.Buffers.Binary;
using MemoryPack;

namespace Phantom.Utils.Rpc.Message;

static class MessageSerializer {
	private static readonly MemoryPackSerializerOptions SerializerOptions = MemoryPackSerializerOptions.Utf8;

	public static byte[] Serialize<T>(T message) {
		return MemoryPackSerializer.Serialize(typeof(T), message, SerializerOptions);
	}

	public static void Serialize<T>(IBufferWriter<byte> destination, T message) {
		MemoryPackSerializer.Serialize(typeof(T), destination, message, SerializerOptions);
	}

	public static T Deserialize<T>(ReadOnlyMemory<byte> memory) {
		return MemoryPackSerializer.Deserialize<T>(memory.Span) ?? throw new NullReferenceException();
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
