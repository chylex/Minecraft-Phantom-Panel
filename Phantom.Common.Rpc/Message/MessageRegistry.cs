using MessagePack;
using Serilog;

namespace Phantom.Common.Rpc.Message;

public sealed class MessageRegistry<TListener, TMessage> where TMessage : class, IMessage<TListener> {
	private readonly ILogger logger;
	private readonly MessagePackSerializerOptions serializerOptions;
	
	private readonly Dictionary<Type, ushort> typeToCodeMapping = new ();
	private readonly Dictionary<ushort, Func<ReadOnlyMemory<byte>, MessagePackSerializerOptions, CancellationToken, TMessage>> codeToDeserializerMapping = new ();

	internal MessageRegistry(ILogger logger) {
		this.logger = logger;
		this.serializerOptions = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.None).WithSecurity(MessagePackSecurity.UntrustedData.WithMaximumObjectGraphDepth(10));
	}

	internal void Add<T>(ushort code, Func<ReadOnlyMemory<byte>, MessagePackSerializerOptions, CancellationToken, TMessage> deserializer) where T : TMessage {
		typeToCodeMapping.Add(typeof(T), code);
		codeToDeserializerMapping.Add(code, deserializer);
	}

	public ReadOnlySpan<byte> Write<T>(T message, CancellationToken cancellationToken = default) where T : TMessage {
		if (!typeToCodeMapping.TryGetValue(typeof(T), out ushort code)) {
			logger.Error("Unknown message type {Type}.", typeof(T));
			return new ReadOnlySpan<byte>();
		}

		var stream = new MemoryStream();
		
		try {
			WriteUshort(stream, code);
			MessagePackSerializer.Serialize(stream, message, serializerOptions, cancellationToken);
			return new ReadOnlySpan<byte>(stream.GetBuffer(), 0, (int) stream.Length);
		} catch (Exception e) {
			logger.Error(e, "Failed to serialize message {Type}.", typeof(T));
			return new ReadOnlySpan<byte>();
		}
	}

	public void Handle(byte[] bytes, TListener listener, CancellationToken cancellationToken) {
		var memory = new ReadOnlyMemory<byte>(bytes);
		
		ushort code;
		try {
			code = ReadUshort(ref memory);
		} catch (Exception e) {
			logger.Error(e, "Failed to deserialize message code.");
			return;
		}

		if (!codeToDeserializerMapping.TryGetValue(code, out var deserialize)) {
			logger.Error("Unknown message code {Code}.", code);
			return;
		}
		
		TMessage message;
		try {
			message = deserialize(memory, serializerOptions, cancellationToken);
		} catch (Exception e) {
			logger.Error(e, "Failed to deserialize message with code {Code}.", code);
			return;
		}

		try {
			message.Accept(listener);
		} catch (Exception e) {
			logger.Error(e, "Failed to handle message {Type}.", message.GetType());
		}
	}

	private static void WriteUshort(MemoryStream stream, ushort value) {
		stream.WriteByte((byte) (value & 0xFF));
		stream.WriteByte((byte) ((value >> 8) & 0xFF));
	}

	private static ushort ReadUshort(ref ReadOnlyMemory<byte> memory) {
		ushort value = (ushort) (memory.Span[0] | (memory.Span[1] << 8));
		memory = memory[2..];
		return value;
	}
}
