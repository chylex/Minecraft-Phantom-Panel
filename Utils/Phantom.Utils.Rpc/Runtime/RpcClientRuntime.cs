using NetMQ.Sockets;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Sockets;
using Phantom.Utils.Tasks;
using Serilog;
using Serilog.Events;

namespace Phantom.Utils.Rpc.Runtime;

public abstract class RpcClientRuntime<TClientListener, TServerListener, TReplyMessage> : RpcRuntime<ClientSocket> where TReplyMessage : IMessage<TClientListener, NoReply>, IMessage<TServerListener, NoReply> {
	private readonly RpcConnectionToServer<TServerListener> connection;
	private readonly IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions;
	private readonly TClientListener messageListener;

	private readonly SemaphoreSlim disconnectSemaphore;
	private readonly CancellationToken receiveCancellationToken;

	protected RpcClientRuntime(RpcClientSocket<TClientListener, TServerListener, TReplyMessage> socket, TClientListener messageListener, SemaphoreSlim disconnectSemaphore, CancellationToken receiveCancellationToken) : base(socket) {
		this.connection = socket.Connection;
		this.messageDefinitions = socket.MessageDefinitions;
		this.messageListener = messageListener;
		this.disconnectSemaphore = disconnectSemaphore;
		this.receiveCancellationToken = receiveCancellationToken;
	}

	private protected sealed override void Run(ClientSocket socket, ILogger logger, MessageReplyTracker replyTracker, TaskManager taskManager) {
		RunWithConnection(socket, connection, logger, taskManager);
	}

	protected virtual void RunWithConnection(ClientSocket socket, RpcConnectionToServer<TServerListener> connection, ILogger logger, TaskManager taskManager) {
		var handler = new Handler(connection, messageDefinitions, messageListener, logger, taskManager, receiveCancellationToken);

		try {
			while (!receiveCancellationToken.IsCancellationRequested) {
				var data = socket.Receive(receiveCancellationToken);
				
				LogMessageType(logger, data);
				
				if (data.Length > 0) {
					messageDefinitions.ToClient.Handle(data, handler);
				}
			}
		} catch (OperationCanceledException) {
			// Ignore.
		} finally {
			logger.Debug("ZeroMQ client stopped receiving messages.");
			disconnectSemaphore.Wait(CancellationToken.None);
		}
	}

	private void LogMessageType(ILogger logger, ReadOnlyMemory<byte> data) {
		if (!logger.IsEnabled(LogEventLevel.Verbose)) {
			return;
		}

		if (data.Length > 0 && messageDefinitions.ToClient.TryGetType(data, out var type)) {
			logger.Verbose("Received {MessageType} ({Bytes} B).", type.Name, data.Length);
		}
		else {
			logger.Verbose("Received {Bytes} B message.", data.Length);
		}
	}

	private sealed class Handler : MessageHandler<TClientListener> {
		private readonly RpcConnectionToServer<TServerListener> connection;
		private readonly IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions;
		
		public Handler(RpcConnectionToServer<TServerListener> connection, IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions, TClientListener listener, ILogger logger, TaskManager taskManager, CancellationToken cancellationToken) : base(listener, logger, taskManager, cancellationToken) {
			this.connection = connection;
			this.messageDefinitions = messageDefinitions;
		}
		
		protected override Task SendReply(uint sequenceId, byte[] serializedReply) {
			return connection.Send(messageDefinitions.CreateReplyMessage(sequenceId, serializedReply));
		}
	}
}
