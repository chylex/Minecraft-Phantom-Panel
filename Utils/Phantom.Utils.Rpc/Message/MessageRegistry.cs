using System.Diagnostics.CodeAnalysis;
using Phantom.Utils.Runtime;
using Serilog;

namespace Phantom.Utils.Rpc.Message;

public sealed class MessageRegistry<TListener, TMessageBase> where TMessageBase : class, IMessage<TListener> {
	private readonly ILogger logger;
	private readonly Dictionary<Type, ushort> typeToCodeMapping = new ();
	private readonly Dictionary<ushort, Type> codeToTypeMapping = new ();
	private readonly Dictionary<ushort, Func<ReadOnlyMemory<byte>, CancellationToken, TMessageBase>> codeToDeserializerMapping = new ();

	public MessageRegistry(ILogger logger) {
		this.logger = logger;
	}

	public void Add<TMessage>(ushort code) where TMessage : TMessageBase {
		typeToCodeMapping.Add(typeof(TMessage), code);
		codeToTypeMapping.Add(code, typeof(TMessage));
		codeToDeserializerMapping.Add(code, MessageSerializer.Deserialize<TMessage, TMessageBase, TListener>());
	}

	public bool TryGetType(byte[] bytes, [NotNullWhen(true)] out Type? type) {
		var memory = new ReadOnlyMemory<byte>(bytes);

		try {
			var code = MessageSerializer.ReadCode(ref memory);
			return codeToTypeMapping.TryGetValue(code, out type);
		} catch (Exception) {
			type = null;
			return false;
		}
	}

	public ReadOnlySpan<byte> Write<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : TMessageBase {
		if (!typeToCodeMapping.TryGetValue(typeof(TMessage), out ushort code)) {
			logger.Error("Unknown message type {Type}.", typeof(TMessage));
			return default;
		}

		var stream = new MemoryStream();

		try {
			MessageSerializer.WriteCode(stream, code);
			MessageSerializer.Serialize<TMessage, TListener>(stream, message, cancellationToken);
			return new ReadOnlySpan<byte>(stream.GetBuffer(), 0, (int) stream.Length);
		} catch (Exception e) {
			logger.Error(e, "Failed to serialize message {Type}.", typeof(TMessage));
			return default;
		}
	}

	public void Handle(byte[] bytes, TListener listener, TaskManager taskManager, CancellationToken cancellationToken) {
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

		cancellationToken.ThrowIfCancellationRequested();
		taskManager.Run(HandleMessage);
	}
}
