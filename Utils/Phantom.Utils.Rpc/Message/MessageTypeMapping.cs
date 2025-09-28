using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Frame.Types;
using Serilog;

namespace Phantom.Utils.Rpc.Message;

sealed class MessageTypeMapping<TMessageBase> {
	private readonly ILogger logger;
	
	private readonly FrozenDictionary<Type, byte> messageTypeToTypeCodeMapping;
	private readonly FrozenDictionary<byte, Registration> messageTypeCodeToRegistrationMapping;
	
	private MessageTypeMapping(string loggerName, FrozenDictionary<Type, byte> messageTypeToTypeCodeMapping, FrozenDictionary<byte, Registration> messageTypeCodeToRegistrationMapping) {
		this.logger = PhantomLogger.Create<MessageTypeMapping<TMessageBase>>(loggerName);
		this.messageTypeToTypeCodeMapping = messageTypeToTypeCodeMapping;
		this.messageTypeCodeToRegistrationMapping = messageTypeCodeToRegistrationMapping;
	}
	
	private readonly record struct Registration(Type MessageType, MessageRegistry<TMessageBase>.DeserializeAndHandleFunc Action);
	
	public bool TryGetType(MessageFrame frame, [NotNullWhen(true)] out Type? type) {
		if (messageTypeCodeToRegistrationMapping.TryGetValue(frame.MessageTypeCode, out var registration)) {
			type = registration.MessageType;
			return true;
		}
		else {
			type = null;
			return false;
		}
	}
	
	public MessageFrame CreateFrame<TMessage>(uint messageId, TMessage message) where TMessage : TMessageBase {
		if (messageTypeToTypeCodeMapping.TryGetValue(typeof(TMessage), out byte messageTypeCode)) {
			return new MessageFrame(messageId, messageTypeCode, MessageSerialization.Serialize(message));
		}
		else {
			throw new ArgumentException("Unknown message type: " + typeof(TMessage));
		}
	}
	
	public async Task Handle(MessageFrame frame, MessageHandler<TMessageBase> handler, CancellationToken cancellationToken) {
		uint messageId = frame.MessageId;
		
		if (messageTypeCodeToRegistrationMapping.TryGetValue(frame.MessageTypeCode, out var registration)) {
			await registration.Action(messageId, frame.SerializedMessage, handler, cancellationToken);
		}
		else {
			logger.Error("Unknown message code {Code} for message {MessageId}.", frame.MessageTypeCode, messageId);
			await handler.SendError(messageId, MessageError.UnknownMessageRegistryCode, cancellationToken);
		}
	}
	
	public sealed class Builder {
		private readonly Dictionary<Type, byte> messageTypeToTypeCodeMapping = new ();
		private readonly Dictionary<byte, Registration> messageTypeCodeToRegistrationMapping = new ();
		
		public void Add(byte messageTypeCode, Type messageType, MessageRegistry<TMessageBase>.DeserializeAndHandleFunc action) {
			messageTypeToTypeCodeMapping.Add(messageType, messageTypeCode);
			messageTypeCodeToRegistrationMapping.Add(messageTypeCode, new Registration(messageType, action));
		}
		
		public MessageTypeMapping<TMessageBase> Build(string loggerName) {
			return new MessageTypeMapping<TMessageBase>(loggerName, messageTypeToTypeCodeMapping.ToFrozenDictionary(), messageTypeCodeToRegistrationMapping.ToFrozenDictionary());
		}
	}
}
