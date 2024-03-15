using NetMQ.Sockets;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Sockets;
using Serilog;
using Serilog.Events;

namespace Phantom.Utils.Rpc.Runtime;

public abstract class RpcClientRuntime<TClientMessage, TServerMessage, TReplyMessage> : RpcRuntime<ClientSocket> where TReplyMessage : TClientMessage, TServerMessage {
	private readonly RpcConnectionToServer<TServerMessage> connection;
	private readonly IMessageDefinitions<TClientMessage, TServerMessage, TReplyMessage> messageDefinitions;
	private readonly ActorRef<TClientMessage> handlerActor;

	private readonly SemaphoreSlim disconnectSemaphore;
	private readonly CancellationToken receiveCancellationToken;

	protected RpcClientRuntime(RpcClientSocket<TClientMessage, TServerMessage, TReplyMessage> socket, ActorRef<TClientMessage> handlerActor, SemaphoreSlim disconnectSemaphore, CancellationToken receiveCancellationToken) : base(socket) {
		this.connection = socket.Connection;
		this.messageDefinitions = socket.MessageDefinitions;
		this.handlerActor = handlerActor;
		this.disconnectSemaphore = disconnectSemaphore;
		this.receiveCancellationToken = receiveCancellationToken;
	}

	private protected sealed override Task Run(ClientSocket socket) {
		return RunWithConnection(socket, connection);
	}

	protected virtual async Task RunWithConnection(ClientSocket socket, RpcConnectionToServer<TServerMessage> connection) {
		var replySender = new ReplySender<TServerMessage, TReplyMessage>(connection, messageDefinitions);
		var messageHandler = new MessageHandler<TClientMessage>(LoggerName, handlerActor, replySender);

		try {
			while (!receiveCancellationToken.IsCancellationRequested) {
				var data = socket.Receive(receiveCancellationToken);
				
				LogMessageType(RuntimeLogger, data);
				
				if (data.Length > 0) {
					messageDefinitions.ToClient.Handle(data, messageHandler);
				}
			}
		} catch (OperationCanceledException) {
			// Ignore.
		} finally {
			await handlerActor.Stop();
			RuntimeLogger.Debug("ZeroMQ client stopped receiving messages.");
			
			await disconnectSemaphore.WaitAsync(CancellationToken.None);
		}
	}

	private protected sealed override async Task Disconnect(ClientSocket socket) {
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
}
