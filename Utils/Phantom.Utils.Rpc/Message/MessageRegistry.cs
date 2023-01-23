using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Events;

namespace Phantom.Utils.Rpc.Message;

public sealed class MessageRegistry<TListener> {
	private const int DefaultBufferSize = 512;

	private readonly ILogger logger;
	private readonly Dictionary<Type, ushort> typeToCodeMapping = new ();
	private readonly Dictionary<ushort, Type> codeToTypeMapping = new ();
	private readonly Dictionary<ushort, Action<ReadOnlyMemory<byte>, ushort, MessageHandler<TListener>>> codeToHandlerMapping = new ();

	public MessageRegistry(ILogger logger) {
		this.logger = logger;
	}

	public void Add<TMessage>(ushort code) where TMessage : IMessage<TListener, NoReply> {
		AddTypeCodeMapping<TMessage, NoReply>(code);
		codeToHandlerMapping.Add(code, DeserializationHandler<TMessage>);
	}

	public void Add<TMessage, TReply>(ushort code) where TMessage : IMessage<TListener, TReply> {
		if (typeof(TReply) == typeof(NoReply)) {
			throw new InvalidOperationException("This overload of Add must not be used with NoReply as the reply type!");
		}
		
		AddTypeCodeMapping<TMessage, TReply>(code);
		codeToHandlerMapping.Add(code, DeserializationHandler<TMessage, TReply>);
	}

	private void AddTypeCodeMapping<TMessage, TReply>(ushort code) where TMessage : IMessage<TListener, TReply> {
		typeToCodeMapping.Add(typeof(TMessage), code);
		codeToTypeMapping.Add(code, typeof(TMessage));
	}

	public bool TryGetType(ReadOnlyMemory<byte> data, [NotNullWhen(true)] out Type? type) {
		try {
			var code = MessageSerializer.ReadCode(ref data);
			return codeToTypeMapping.TryGetValue(code, out type);
		} catch (Exception) {
			type = null;
			return false;
		}
	}

	public ReadOnlySpan<byte> Write<TMessage>(TMessage message) where TMessage : IMessage<TListener, NoReply> {
		return Write<TMessage, NoReply>(0, message);
	}

	public ReadOnlySpan<byte> Write<TMessage, TReply>(uint sequenceId, TMessage message) where TMessage : IMessage<TListener, TReply> {
		if (!typeToCodeMapping.TryGetValue(typeof(TMessage), out ushort code)) {
			logger.Error("Unknown message type {Type}.", typeof(TMessage));
			return default;
		}

		var buffer = new ArrayBufferWriter<byte>(DefaultBufferSize);

		try {
			MessageSerializer.WriteCode(buffer, code);

			if (typeof(TReply) != typeof(NoReply)) {
				MessageSerializer.WriteSequenceId(buffer, sequenceId);
			}

			MessageSerializer.Serialize(buffer, message);

			if (buffer.WrittenCount > DefaultBufferSize && logger.IsEnabled(LogEventLevel.Verbose)) {
				logger.Verbose("Serializing {Type} exceeded default buffer size: {WrittenSize} B > {DefaultBufferSize} B", typeof(TMessage).Name, buffer.WrittenCount, DefaultBufferSize);
			}

			return buffer.WrittenSpan;
		} catch (Exception e) {
			logger.Error(e, "Failed to serialize message {Type}.", typeof(TMessage).Name);
			return default;
		}
	}

	public void Handle(ReadOnlyMemory<byte> data, MessageHandler<TListener> handler) {
		ushort code;
		try {
			code = MessageSerializer.ReadCode(ref data);
		} catch (Exception e) {
			logger.Error(e, "Failed to deserialize message code.");
			return;
		}

		if (!codeToHandlerMapping.TryGetValue(code, out var handle)) {
			logger.Error("Unknown message code {Code}.", code);
			return;
		}

		handle(data, code, handler);
	}

	private void DeserializationHandler<TMessage>(ReadOnlyMemory<byte> data, ushort code, MessageHandler<TListener> handler) where TMessage : IMessage<TListener, NoReply> {
		DeserializeAndEnqueueMessage<TMessage, NoReply>(data, code, handler, 0);
	}

	private void DeserializationHandler<TMessage, TReply>(ReadOnlyMemory<byte> data, ushort code, MessageHandler<TListener> handler) where TMessage : IMessage<TListener, TReply> {
		uint sequenceId;
		try {
			sequenceId = MessageSerializer.ReadSequenceId(ref data);
		} catch (Exception e) {
			logger.Error(e, "Failed to deserialize sequence ID of message with code {Code}.", code);
			return;
		}

		DeserializeAndEnqueueMessage<TMessage, TReply>(data, code, handler, sequenceId);
	}

	private void DeserializeAndEnqueueMessage<TMessage, TReply>(ReadOnlyMemory<byte> data, ushort code, MessageHandler<TListener> handler, uint sequenceId) where TMessage : IMessage<TListener, TReply> {
		TMessage message;
		try {
			message = MessageSerializer.Deserialize<TMessage>(data);
		} catch (Exception e) {
			logger.Error(e, "Failed to deserialize message with code {Code}.", code);
			return;
		}

		handler.Enqueue<TMessage, TReply>(sequenceId, message);
	}
}
