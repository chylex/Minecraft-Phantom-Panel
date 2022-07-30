using BinaryPack;

namespace Phantom.Common.Rpc.Message;

public sealed class MessageRegistry<TListener, TMessage> where TMessage : IMessage<TListener> {
	private readonly Dictionary<Type, ushort> typeToCodeMapping = new ();
	private readonly Dictionary<ushort, Func<MemoryStream, TMessage>> codeToDeserializerMapping = new ();

	internal void Add<T>(ushort code, Func<MemoryStream, TMessage> deserializer) where T : TMessage {
		typeToCodeMapping.Add(typeof(T), code);
		codeToDeserializerMapping.Add(code, deserializer);
	}

	public ReadOnlySpan<byte> Write<T>(T message) where T : TMessage, new() {
		var code = typeToCodeMapping[typeof(T)];
		var stream = new MemoryStream();
		WriteUshort(stream, code);
		BinaryConverter.Serialize(message, stream);
		return new ReadOnlySpan<byte>(stream.GetBuffer(), 0, (int) stream.Length);
	}

	public void Handle(byte[] bytes, TListener listener) {
		var stream = new MemoryStream(bytes);
		var code = ReadUshort(stream);
		var deserializer = codeToDeserializerMapping[code];
		var message = deserializer(stream);
		message.Accept(listener);
	}

	private static void WriteUshort(MemoryStream stream, ushort value) {
		stream.WriteByte((byte) (value & 0xFF));
		stream.WriteByte((byte) ((value >> 8) & 0xFF));
	}

	private static ushort ReadUshort(MemoryStream stream) {
		return (ushort) (stream.ReadByte() | (stream.ReadByte() << 8));
	}
}
