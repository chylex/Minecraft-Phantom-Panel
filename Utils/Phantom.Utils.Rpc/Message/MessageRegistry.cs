using Serilog;

namespace Phantom.Utils.Rpc.Message;

public sealed class MessageRegistry<TListener, TMessageBase> where TMessageBase : class, IMessage<TListener> {
	private readonly ILogger logger;
	private readonly Dictionary<Type, ushort> typeToCodeMapping = new ();
	private readonly Dictionary<ushort, Func<ReadOnlyMemory<byte>, CancellationToken, TMessageBase>> codeToDeserializerMapping = new ();
	private readonly HashSet<ushort> codesWithReplies = new ();
	private readonly Dictionary<uint, Action<object>> replyHandlers = new ();
	private uint lastSequenceId;

	public MessageRegistry(ILogger logger) {
		this.logger = logger;
	}

	public void Add<TMessage>(ushort code) where TMessage : TMessageBase {
		typeToCodeMapping.Add(typeof(TMessage), code);
		codeToDeserializerMapping.Add(code, MessageSerializer.Deserialize<TMessage, TMessageBase, TListener>());
	}

	// public void Add<TMessage, TReply>(ushort code) where TMessage : TMessageBase, IMessageWithReply<TListener, TReply> {
	// 	Add<TMessage>(code);
	// 	codesWithReplies.Add(code);
	// }

	public ReadOnlySpan<byte> Write<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : TMessageBase {
		if (!typeToCodeMapping.TryGetValue(typeof(TMessage), out ushort code)) {
			logger.Error("Unknown message type {Type}.", typeof(TMessage));
			return default;
		}

		var stream = new MemoryStream();

		try {
			MessageSerializer.WriteCode(stream, code);

			// if (codesWithReplies.Contains(code)) {
			// 	var sequenceId = Interlocked.Increment(ref lastSequenceId);
			// 	MessageSerializer.WriteSequenceId(stream, sequenceId);
			// }
			
			MessageSerializer.Serialize<TMessage, TListener>(stream, message, cancellationToken);
			return new ReadOnlySpan<byte>(stream.GetBuffer(), 0, (int) stream.Length);
		} catch (Exception e) {
			logger.Error(e, "Failed to serialize message {Type}.", typeof(TMessage));
			return default;
		}
	}

	public ReadOnlySpan<byte> WriteWithSequenceId<TMessage, TParam>(Func<uint, TParam, TMessage> messageFactory, TParam factoryParameter, CancellationToken cancellationToken = default) where TMessage : TMessageBase {
		return Write(messageFactory(Interlocked.Increment(ref lastSequenceId), factoryParameter), cancellationToken);
	}

	public void Handle(byte[] bytes, TListener listener, CancellationToken cancellationToken) {
		var memory = new ReadOnlyMemory<byte>(bytes);

		ushort code;
		try {
			code = MessageSerializer.ReadCode(ref memory);
		} catch (Exception e) {
			logger.Error(e, "Failed to deserialize message code.");
			return;
		}

		if (!codeToDeserializerMapping.TryGetValue(code, out var deserialize)) {
			logger.Error("Unknown message code {Code}.", code);
			return;
		}

		// uint? sequenceId = null;
		// if (codesWithReplies.Contains(code)) {
		// 	try {
		// 		sequenceId = MessageSerializer.ReadSequenceId(ref memory);
		// 	} catch (Exception e) {
		// 		logger.Error(e, "Failed to deserialize sequence id.");
		// 		return;
		// 	}
		// }

		TMessageBase message;
		try {
			message = deserialize(memory, cancellationToken);
		} catch (Exception e) {
			logger.Error(e, "Failed to deserialize message with code {Code}.", code);
			return;
		}

		async Task HandleMessage() {
			try {
				await message.Accept(listener);
			} catch (Exception e) {
				logger.Error(e, "Failed to handle message {Type}.", message.GetType());
			}
		}

		Task.Run(HandleMessage, cancellationToken);
	}
}
