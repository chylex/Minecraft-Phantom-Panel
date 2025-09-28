using System.Collections.Immutable;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Runtime;
using Serilog;

namespace Phantom.Utils.Rpc.Message;

public sealed class MessageRegistry<TMessageBase>(string loggerName) {
	private readonly ILogger logger = PhantomLogger.Create<MessageRegistry<TMessageBase>>(loggerName);
	private readonly List<MessageInfo> messageInfoList = [];
	
	private readonly record struct MessageInfo(Type Type, MessageTypeName TypeName, DeserializeAndHandleFunc Action);
	
	internal delegate Task DeserializeAndHandleFunc(uint messageId, ReadOnlyMemory<byte> serializedMessage, MessageHandler<TMessageBase> handler, CancellationToken cancellationToken);
	
	public void Add<TMessage>() where TMessage : TMessageBase {
		if (HasReplyType(typeof(TMessage))) {
			throw new ArgumentException("This overload is for messages without a reply.");
		}
		
		AddImpl(typeof(TMessage), DeserializationHandler<TMessage>);
	}
	
	public void Add<TMessage, TReply>() where TMessage : TMessageBase, ICanReply<TReply> {
		AddImpl(typeof(TMessage), DeserializationHandler<TMessage, TReply>);
	}
	
	private void AddImpl(Type messageType, DeserializeAndHandleFunc action) {
		messageInfoList.Add(new MessageInfo(messageType, new MessageTypeName(messageType.Name), action));
	}
	
	private static bool HasReplyType(Type messageType) {
		string replyInterfaceName = typeof(ICanReply<object>).FullName!;
		replyInterfaceName = replyInterfaceName[..(replyInterfaceName.IndexOf('`') + 1)];
		
		return messageType.GetInterfaces().Any(type => type.FullName is {} name && name.StartsWith(replyInterfaceName, StringComparison.Ordinal));
	}
	
	internal WithMapping CreateMapping() {
		var messageTypeNames = ImmutableArray.CreateBuilder<MessageTypeName>();
		var messageTypeMapping = new MessageTypeMapping<TMessageBase>.Builder();
		
		int nextMessageCode = 0;
		
		foreach ((Type messageType, MessageTypeName messageTypeName, DeserializeAndHandleFunc action) in messageInfoList) {
			if (nextMessageCode == byte.MaxValue) {
				throw new InvalidOperationException("Trying to register too many messages (" + (nextMessageCode + 1) + ").");
			}
			
			messageTypeNames.Add(messageTypeName);
			messageTypeMapping.Add((byte) nextMessageCode++, messageType, action);
		}
		
		return new WithMapping(messageTypeNames.ToImmutable(), messageTypeMapping.Build(loggerName));
	}
	
	internal sealed class WithMapping(ImmutableArray<MessageTypeName> messageTypeNames, MessageTypeMapping<TMessageBase> mapping) {
		public MessageTypeMapping<TMessageBase> Mapping => mapping;
		
		public async ValueTask Write(RpcStream stream, CancellationToken cancellationToken) {
			foreach (MessageTypeName typeName in messageTypeNames) {
				await typeName.Write(stream, cancellationToken);
			}
			
			await MessageTypeName.WriteEnd(stream, cancellationToken);
		}
	}
	
	internal async ValueTask<ReadMappingResult> ReadMapping(RpcStream stream, CancellationToken cancellationToken) {
		var messageTypeNameToInfoMapping = messageInfoList.ToImmutableDictionary(static item => item.TypeName, static item => item);
		
		var messageTypeMapping = new MessageTypeMapping<TMessageBase>.Builder();
		var supportedMessages = ImmutableSortedDictionary.CreateBuilder<byte, MessageTypeName>();
		var unsupportedMessages = ImmutableSortedDictionary.CreateBuilder<byte, MessageTypeName>();
		
		byte nextMessageCode = 0;
		
		while (await MessageTypeName.Read(stream, cancellationToken) is {} messageTypeName) {
			if (nextMessageCode == byte.MaxValue) {
				throw new InvalidOperationException("Trying to register too many messages (" + (nextMessageCode + 1) + ").");
			}
			
			if (messageTypeNameToInfoMapping.TryGetValue(messageTypeName, out var messageInfo)) {
				messageTypeMapping.Add(nextMessageCode, messageInfo.Type, messageInfo.Action);
				supportedMessages.Add(nextMessageCode, messageTypeName);
			}
			else {
				unsupportedMessages.Add(nextMessageCode, messageTypeName);
			}
			
			++nextMessageCode;
		}
		
		return new ReadMappingResult(messageTypeMapping.Build(loggerName), supportedMessages.ToImmutable(), unsupportedMessages.ToImmutable());
	}
	
	internal readonly record struct ReadMappingResult(
		MessageTypeMapping<TMessageBase> TypeMapping,
		ImmutableSortedDictionary<byte, MessageTypeName> SupportedMessages,
		ImmutableSortedDictionary<byte, MessageTypeName> UnsupportedMessages
	);
	
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
