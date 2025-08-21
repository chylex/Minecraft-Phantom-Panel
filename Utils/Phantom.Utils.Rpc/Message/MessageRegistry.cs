using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Phantom.Utils.Actor;
using Serilog;
using Serilog.Events;

namespace Phantom.Utils.Rpc.Message;

public sealed class MessageRegistry<TMessageBase> {
	private const int DefaultBufferSize = 512;
	
	private readonly ILogger logger;
	private readonly Dictionary<Type, ushort> typeToCodeMapping = new ();
	private readonly Dictionary<ushort, Type> codeToTypeMapping = new ();
	private readonly Dictionary<ushort, Action<ReadOnlyMemory<byte>, ushort, MessageHandler<TMessageBase>>> codeToHandlerMapping = new ();
	
	public MessageRegistry(ILogger logger) {
		this.logger = logger;
	}
	
	public void Add<TMessage>(ushort code) where TMessage : TMessageBase {
		if (HasReplyType(typeof(TMessage))) {
			throw new ArgumentException("This overload is for messages without a reply");
		}
		
		AddTypeCodeMapping<TMessage>(code);
		codeToHandlerMapping.Add(code, DeserializationHandler<TMessage>);
	}
	
	public void Add<TMessage, TReply>(ushort code) where TMessage : TMessageBase, ICanReply<TReply> {
		AddTypeCodeMapping<TMessage>(code);
		codeToHandlerMapping.Add(code, DeserializationHandler<TMessage, TReply>);
	}
	
	private void AddTypeCodeMapping<TMessage>(ushort code) where TMessage : TMessageBase {
		typeToCodeMapping.Add(typeof(TMessage), code);
		codeToTypeMapping.Add(code, typeof(TMessage));
	}
	
	private bool HasReplyType(Type messageType) {
		string replyInterfaceName = typeof(ICanReply<object>).FullName!;
		replyInterfaceName = replyInterfaceName[..(replyInterfaceName.IndexOf('`') + 1)];
		
		return messageType.GetInterfaces().Any(type => type.FullName is {} name && name.StartsWith(replyInterfaceName, StringComparison.Ordinal));
	}
	
	internal bool TryGetType(ReadOnlyMemory<byte> data, [NotNullWhen(true)] out Type? type) {
		try {
			var code = MessageSerializer.ReadCode(ref data);
			return codeToTypeMapping.TryGetValue(code, out type);
		} catch (Exception) {
			type = null;
			return false;
		}
	}
	
	public ReadOnlySpan<byte> Write<TMessage>(TMessage message) where TMessage : TMessageBase {
		if (!GetMessageCode<TMessage>(out var code)) {
			return default;
		}
		
		var buffer = new ArrayBufferWriter<byte>(DefaultBufferSize);
		
		try {
			MessageSerializer.WriteCode(buffer, code);
			MessageSerializer.Serialize(buffer, message);
			
			CheckWrittenBufferLength<TMessage>(buffer);
			return buffer.WrittenSpan;
		} catch (Exception e) {
			LogWriteFailure<TMessage>(e);
			return default;
		}
	}
	
	public ReadOnlySpan<byte> Write<TMessage, TReply>(uint sequenceId, TMessage message) where TMessage : TMessageBase, ICanReply<TReply> {
		if (!GetMessageCode<TMessage>(out var code)) {
			return default;
		}
		
		var buffer = new ArrayBufferWriter<byte>(DefaultBufferSize);
		
		try {
			MessageSerializer.WriteCode(buffer, code);
			MessageSerializer.WriteSequenceId(buffer, sequenceId);
			MessageSerializer.Serialize(buffer, message);
			
			CheckWrittenBufferLength<TMessage>(buffer);
			return buffer.WrittenSpan;
		} catch (Exception e) {
			LogWriteFailure<TMessage>(e);
			return default;
		}
	}
	
	private bool GetMessageCode<TMessage>(out ushort code) where TMessage : TMessageBase {
		if (typeToCodeMapping.TryGetValue(typeof(TMessage), out code)) {
			return true;
		}
		else {
			logger.Error("Unknown message type {Type}.", typeof(TMessage));
			return false;
		}
	}
	
	private void CheckWrittenBufferLength<TMessage>(ArrayBufferWriter<byte> buffer) where TMessage : TMessageBase {
		if (buffer.WrittenCount > DefaultBufferSize && logger.IsEnabled(LogEventLevel.Verbose)) {
			logger.Verbose("Serializing {Type} exceeded default buffer size: {WrittenSize} B > {DefaultBufferSize} B", typeof(TMessage).Name, buffer.WrittenCount, DefaultBufferSize);
		}
	}
	
	private void LogWriteFailure<TMessage>(Exception e) where TMessage : TMessageBase {
		logger.Error(e, "Failed to serialize message {Type}.", typeof(TMessage).Name);
	}
	
	internal bool Read<TMessage>(ReadOnlyMemory<byte> data, out TMessage message) where TMessage : TMessageBase {
		if (ReadTypeCode(ref data, out ushort code) && codeToTypeMapping.TryGetValue(code, out var expectedType) && expectedType == typeof(TMessage) && ReadMessage(data, out message)) {
			return true;
		}
		else {
			message = default!;
			return false;
		}
	}
	
	internal void Handle(ReadOnlyMemory<byte> data, MessageHandler<TMessageBase> handler) {
		if (!ReadTypeCode(ref data, out var code)) {
			return;
		}
		
		if (!codeToHandlerMapping.TryGetValue(code, out var handle)) {
			logger.Error("Unknown message code {Code}.", code);
			return;
		}
		
		handle(data, code, handler);
	}
	
	private bool ReadTypeCode(ref ReadOnlyMemory<byte> data, out ushort code) {
		try {
			code = MessageSerializer.ReadCode(ref data);
			return true;
		} catch (Exception e) {
			code = default;
			logger.Error(e, "Failed to deserialize message code.");
			return false;
		}
	}
	
	private bool ReadSequenceId<TMessage, TReply>(ref ReadOnlyMemory<byte> data, out uint sequenceId) where TMessage : TMessageBase, ICanReply<TReply> {
		try {
			sequenceId = MessageSerializer.ReadSequenceId(ref data);
			return true;
		} catch (Exception e) {
			sequenceId = default;
			logger.Error(e, "Failed to deserialize sequence ID of message {Type}.", typeof(TMessage).Name);
			return false;
		}
	}
	
	private bool ReadMessage<TMessage>(ReadOnlyMemory<byte> data, out TMessage message) where TMessage : TMessageBase {
		try {
			message = MessageSerializer.Deserialize<TMessage>(data);
			return true;
		} catch (Exception e) {
			message = default!;
			logger.Error(e, "Failed to deserialize message {Type}.", typeof(TMessage).Name);
			return false;
		}
	}
	
	private void DeserializationHandler<TMessage>(ReadOnlyMemory<byte> data, ushort code, MessageHandler<TMessageBase> handler) where TMessage : TMessageBase {
		if (ReadMessage<TMessage>(data, out var message)) {
			handler.Tell(message);
		}
	}
	
	private void DeserializationHandler<TMessage, TReply>(ReadOnlyMemory<byte> data, ushort code, MessageHandler<TMessageBase> handler) where TMessage : TMessageBase, ICanReply<TReply> {
		if (ReadSequenceId<TMessage, TReply>(ref data, out var sequenceId) && ReadMessage<TMessage>(data, out var message)) {
			handler.TellAndReply<TMessage, TReply>(message, sequenceId);
		}
	}
}
