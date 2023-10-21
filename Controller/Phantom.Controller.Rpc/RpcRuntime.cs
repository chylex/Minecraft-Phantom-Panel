using NetMQ.Sockets;
using Phantom.Utils.Rpc;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Sockets;
using Phantom.Utils.Tasks;
using Serilog;
using Serilog.Events;

namespace Phantom.Controller.Rpc;

public static class RpcRuntime {
	public static Task Launch<TClientListener, TServerListener, TReplyMessage>(RpcConfiguration config, IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions, Func<RpcConnectionToClient<TClientListener>, TServerListener> listenerFactory, CancellationToken cancellationToken) where TReplyMessage : IMessage<TClientListener, NoReply>, IMessage<TServerListener, NoReply> {
		return RpcRuntime<TClientListener, TServerListener, TReplyMessage>.Launch(config, messageDefinitions, listenerFactory, cancellationToken);
	}
}

internal sealed class RpcRuntime<TClientListener, TServerListener, TReplyMessage> : RpcRuntime<ServerSocket> where TReplyMessage : IMessage<TClientListener, NoReply>, IMessage<TServerListener, NoReply> {
	internal static Task Launch(RpcConfiguration config, IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions, Func<RpcConnectionToClient<TClientListener>, TServerListener> listenerFactory, CancellationToken cancellationToken) {
		var socket = RpcServerSocket.Connect(config);
		return new RpcRuntime<TClientListener, TServerListener, TReplyMessage>(socket, messageDefinitions, listenerFactory, cancellationToken).Launch();
	}

	private readonly IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions;
	private readonly Func<RpcConnectionToClient<TClientListener>, TServerListener> listenerFactory;
	private readonly CancellationToken cancellationToken;

	private RpcRuntime(RpcServerSocket socket, IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions, Func<RpcConnectionToClient<TClientListener>, TServerListener> listenerFactory, CancellationToken cancellationToken) : base(socket) {
		this.messageDefinitions = messageDefinitions;
		this.listenerFactory = listenerFactory;
		this.cancellationToken = cancellationToken;
	}

	protected override void Run(ServerSocket socket, ILogger logger, MessageReplyTracker replyTracker, TaskManager taskManager) {
		var clients = new Dictionary<ulong, Client>();

		void OnConnectionClosed(object? sender, RpcClientConnectionClosedEventArgs e) {
			clients.Remove(e.RoutingId);
			logger.Debug("Closed connection to {RoutingId}.", e.RoutingId);
		}

		while (!cancellationToken.IsCancellationRequested) {
			var (routingId, data) = socket.Receive(cancellationToken);

			if (data.Length == 0) {
				LogMessageType(logger, routingId, data);
				continue;
			}

			if (!clients.TryGetValue(routingId, out var client)) {
				if (!CheckIsRegistrationMessage(data, logger, routingId)) {
					continue;
				}

				var connection = new RpcConnectionToClient<TClientListener>(socket, routingId, messageDefinitions.ToClient, replyTracker);
				connection.Closed += OnConnectionClosed;

				client = new Client(connection, messageDefinitions, listenerFactory(connection), logger, taskManager, cancellationToken);
				clients[routingId] = client;
			}

			LogMessageType(logger, routingId, data);
			messageDefinitions.ToServer.Handle(data, client);
		}

		foreach (var client in clients.Values) {
			client.Connection.Closed -= OnConnectionClosed;
		}
	}

	private void LogMessageType(ILogger logger, uint routingId, ReadOnlyMemory<byte> data) {
		if (!logger.IsEnabled(LogEventLevel.Verbose)) {
			return;
		}

		if (data.Length > 0 && messageDefinitions.ToServer.TryGetType(data, out var type)) {
			logger.Verbose("Received {MessageType} ({Bytes} B) from {RoutingId}.", type.Name, data.Length, routingId);
		}
		else {
			logger.Verbose("Received {Bytes} B message from {RoutingId}.", data.Length, routingId);
		}
	}

	private bool CheckIsRegistrationMessage(ReadOnlyMemory<byte> data, ILogger logger, uint routingId) {
		if (messageDefinitions.ToServer.TryGetType(data, out var type) && messageDefinitions.IsRegistrationMessage(type)) {
			return true;
		}

		logger.Warning("Received {MessageType} from {RoutingId} who is not registered.", type?.Name ?? "unknown message", routingId);
		return false;
	}
	
	private sealed class Client : MessageHandler<TServerListener> {
		public RpcConnectionToClient<TClientListener> Connection { get; }
		
		private readonly IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions;
		
		public Client(RpcConnectionToClient<TClientListener> connection, IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions, TServerListener listener, ILogger logger, TaskManager taskManager, CancellationToken cancellationToken) : base(listener, logger, taskManager, cancellationToken) {
			this.Connection = connection;
			this.messageDefinitions = messageDefinitions;
		}
	
		protected override Task SendReply(uint sequenceId, byte[] serializedReply) {
			return Connection.Send(messageDefinitions.CreateReplyMessage(sequenceId, serializedReply));
		}
	}
}
