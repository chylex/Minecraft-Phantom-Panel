using System.Diagnostics.CodeAnalysis;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Frame.Types;
using Serilog;

namespace Phantom.Utils.Rpc.Message;

public sealed class MessageRegistry<TMessageBase>(string loggerName) {
	private readonly ILogger logger = PhantomLogger.Create<MessageRegistry<TMessageBase>>(loggerName);
	private readonly Dictionary<Type, ushort> typeToCodeMapping = new ();
	private readonly Dictionary<ushort, Registration> codeToRegistrationMapping = new ();
	
	private readonly record struct Registration(Type MessageType, Func<uint, ReadOnlyMemory<byte>, MessageHandler<TMessageBase>, CancellationToken, Task> Handler);
	
	public void Add<TMessage>(ushort code) where TMessage : TMessageBase {
		Type messageType = typeof(TMessage);
		
		if (HasReplyType(messageType)) {
			throw new ArgumentException("This overload is for messages without a reply.");
		}
		
		typeToCodeMapping.Add(messageType, code);
		codeToRegistrationMapping.Add(code, new Registration(messageType, DeserializationHandler<TMessage>));
	}
	
	public void Add<TMessage, TReply>(ushort code) where TMessage : TMessageBase, ICanReply<TReply> {
		Type messageType = typeof(TMessage);
		
		typeToCodeMapping.Add(messageType, code);
		codeToRegistrationMapping.Add(code, new Registration(messageType, DeserializationHandler<TMessage, TReply>));
	}
	
	private bool HasReplyType(Type messageType) {
		string replyInterfaceName = typeof(ICanReply<object>).FullName!;
		replyInterfaceName = replyInterfaceName[..(replyInterfaceName.IndexOf('`') + 1)];
		
		return messageType.GetInterfaces().Any(type => type.FullName is {} name && name.StartsWith(replyInterfaceName, StringComparison.Ordinal));
	}
	
	internal bool TryGetType(MessageFrame frame, [NotNullWhen(true)] out Type? type) {
		if (codeToRegistrationMapping.TryGetValue(frame.RegistryCode, out var registration)) {
			type = registration.MessageType;
			return true;
		}
		else {
			type = null;
			return false;
		}
	}
	
	internal MessageFrame CreateFrame<TMessage>(uint messageId, TMessage message) where TMessage : TMessageBase {
		if (typeToCodeMapping.TryGetValue(typeof(TMessage), out ushort code)) {
			return new MessageFrame(messageId, code, MessageSerialization.Serialize(message));
		}
		else {
			throw new ArgumentException("Unknown message type: " + typeof(TMessage));
		}
	}
	
	internal async Task Handle(MessageFrame frame, MessageHandler<TMessageBase> handler, CancellationToken cancellationToken) {
		uint messageId = frame.MessageId;
		
		if (codeToRegistrationMapping.TryGetValue(frame.RegistryCode, out var registration)) {
			await registration.Handler(messageId, frame.SerializedMessage, handler, cancellationToken);
		}
		else {
			logger.Error("Unknown message code {Code} for message {MessageId}.", frame.RegistryCode, messageId);
			await handler.SendError(messageId, MessageError.UnknownMessageRegistryCode, cancellationToken);
		}
	}
	
	private async Task DeserializationHandler<TMessage>(uint messageId, ReadOnlyMemory<byte> serializedMessage, MessageHandler<TMessageBase> handler, CancellationToken cancellationToken) where TMessage : TMessageBase {
		TMessage message;
		try {
			message = MessageSerialization.Deserialize<TMessage>(serializedMessage);
		} catch (Exception e) {
			await OnMessageDeserializationError<TMessage>(messageId, e, handler, cancellationToken);
			return;
		}
		
		try {
			handler.Receiver.OnMessage(message);
		} catch (Exception e) {
			await OnMessageHandlingError<TMessage>(messageId, e, handler, cancellationToken);
			return;
		}
		
		try {
			await handler.SendEmptyReply(messageId, cancellationToken);
		} catch (Exception e) {
			await OnMessageReplyingError<TMessage>(messageId, e, handler, cancellationToken);
		}
	}
	
	private async Task DeserializationHandler<TMessage, TReply>(uint messageId, ReadOnlyMemory<byte> serializedMessage, MessageHandler<TMessageBase> handler, CancellationToken cancellationToken) where TMessage : TMessageBase, ICanReply<TReply> {
		TMessage message;
		try {
			message = MessageSerialization.Deserialize<TMessage>(serializedMessage);
		} catch (Exception e) {
			await OnMessageDeserializationError<TMessage>(messageId, e, handler, cancellationToken);
			return;
		}
		
		TReply reply;
		try {
			reply = await handler.Receiver.OnMessage<TMessage, TReply>(message, cancellationToken);
		} catch (Exception e) {
			await OnMessageHandlingError<TMessage>(messageId, e, handler, cancellationToken);
			return;
		}
		
		try {
			await handler.SendReply(messageId, reply, cancellationToken);
		} catch (Exception e) {
			await OnMessageReplyingError<TMessage>(messageId, e, handler, cancellationToken);
		}
	}
	
	private async Task OnMessageDeserializationError<TMessage>(uint messageId, Exception exception, MessageHandler<TMessageBase> handler, CancellationToken cancellationToken) where TMessage : TMessageBase {
		logger.Error(exception, "Could not deserialize message {MessageId} ({MessageType}).", messageId, typeof(TMessage).Name);
		await handler.SendError(messageId, MessageError.MessageDeserializationError, cancellationToken);
	}
	
	private async Task OnMessageHandlingError<TMessage>(uint messageId, Exception exception, MessageHandler<TMessageBase> handler, CancellationToken cancellationToken) where TMessage : TMessageBase {
		logger.Error(exception, "Could not handle message {MessageId} ({MessageType}).", messageId, typeof(TMessage).Name);
		await handler.SendError(messageId, MessageError.MessageHandlingError, cancellationToken);
	}
	
	private async Task OnMessageReplyingError<TMessage>(uint messageId, Exception exception, MessageHandler<TMessageBase> handler, CancellationToken cancellationToken) where TMessage : TMessageBase {
		logger.Error(exception, "Could not reply to message {MessageId} ({MessageType}).", messageId, typeof(TMessage).Name);
		await handler.SendError(messageId, MessageError.MessageReplyingError, cancellationToken);
	}
}
