using NetMQ.Sockets;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Sockets;
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

	private protected sealed override Task Run(ClientSocket socket) {
		return RunWithConnection(socket, connection);
	}

	protected virtual async Task RunWithConnection(ClientSocket socket, RpcConnectionToServer<TServerListener> connection) {
		var handler = new Handler(LoggerName, connection, messageDefinitions, messageListener);

		try {
			while (!receiveCancellationToken.IsCancellationRequested) {
				var data = socket.Receive(receiveCancellationToken);
				
				LogMessageType(RuntimeLogger, data);
				
				if (data.Length > 0) {
					messageDefinitions.ToClient.Handle(data, handler);
				}
			}
		} catch (OperationCanceledException) {
			// Ignore.
		} finally {
			await handler.StopReceiving();
			RuntimeLogger.Debug("ZeroMQ client stopped receiving messages.");
			
			await disconnectSemaphore.WaitAsync(CancellationToken.None);
		}
	}

	private protected sealed override async Task Disconnect(ClientSocket socket) {
		try {
			await connection.StopSending().WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
		} catch (TimeoutException) {
			RuntimeLogger.Error("Timed out waiting for message sending queue.");
		}

		await SendDisconnectMessage(socket, RuntimeLogger);
	}
	
	protected abstract Task SendDisconnectMessage(ClientSocket socket, ILogger logger);

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
		
		public Handler(string loggerName, RpcConnectionToServer<TServerListener> connection, IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions, TClientListener listener) : base(loggerName, listener) {
			this.connection = connection;
			this.messageDefinitions = messageDefinitions;
		}
		
		protected override Task SendReply(uint sequenceId, byte[] serializedReply) {
			return connection.Send(messageDefinitions.CreateReplyMessage(sequenceId, serializedReply));
		}
	}
}
