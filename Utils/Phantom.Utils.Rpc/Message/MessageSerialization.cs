using System.Buffers;
using MemoryPack;

namespace Phantom.Utils.Rpc.Message;

public static class MessageSerialization {
	private static readonly MemoryPackSerializerOptions SerializerOptions = MemoryPackSerializerOptions.Utf8;
	
	public static ReadOnlyMemory<byte> Serialize<T>(T value) {
		var buffer = new ArrayBufferWriter<byte>();
		MemoryPackSerializer.Serialize(buffer, value, SerializerOptions);
		return buffer.WrittenMemory;
	}
	
	public static T Deserialize<T>(ReadOnlyMemory<byte> buffer) {
		return MemoryPackSerializer.Deserialize<T>(buffer.Span, SerializerOptions)!;
	}
}
