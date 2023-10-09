using NetMQ.Sockets;
using Phantom.Utils.Rpc;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Tasks;
using Serilog;
using Serilog.Events;

namespace Phantom.Controller.Rpc;

public static class RpcRuntime {
	public static Task Launch<TOutgoingListener, TIncomingListener, TReplyMessage>(RpcConfiguration config, IMessageDefinitions<TOutgoingListener, TIncomingListener, TReplyMessage> messageDefinitions, Func<RpcClientConnection<TOutgoingListener>, TIncomingListener> listenerFactory, CancellationToken cancellationToken) where TReplyMessage : IMessage<TOutgoingListener, NoReply>, IMessage<TIncomingListener, NoReply> {
		return RpcRuntime<TOutgoingListener, TIncomingListener, TReplyMessage>.Launch(config, messageDefinitions, listenerFactory, cancellationToken);
	}
}

internal sealed class RpcRuntime<TOutgoingListener, TIncomingListener, TReplyMessage> : RpcRuntime<ServerSocket> where TReplyMessage : IMessage<TOutgoingListener, NoReply>, IMessage<TIncomingListener, NoReply> {
	internal static Task Launch(RpcConfiguration config, IMessageDefinitions<TOutgoingListener, TIncomingListener, TReplyMessage> messageDefinitions, Func<RpcClientConnection<TOutgoingListener>, TIncomingListener> listenerFactory, CancellationToken cancellationToken) {
		return new RpcRuntime<TOutgoingListener, TIncomingListener, TReplyMessage>(config, messageDefinitions, listenerFactory, cancellationToken).Launch();
	}

	private static ServerSocket CreateSocket(RpcConfiguration config) {
		var socket = new ServerSocket();
		var options = socket.Options;

		options.CurveServer = true;
		options.CurveCertificate = config.ServerCertificate;
		
		return socket;
	}
	
	private readonly RpcConfiguration config;
	private readonly IMessageDefinitions<TOutgoingListener, TIncomingListener, TReplyMessage> messageDefinitions;
	private readonly Func<RpcClientConnection<TOutgoingListener>, TIncomingListener> listenerFactory;
	private readonly CancellationToken cancellationToken;

	private RpcRuntime(RpcConfiguration config, IMessageDefinitions<TOutgoingListener, TIncomingListener, TReplyMessage> messageDefinitions, Func<RpcClientConnection<TOutgoingListener>, TIncomingListener> listenerFactory, CancellationToken cancellationToken) : base(config, CreateSocket(config)) {
		this.config = config;
		this.messageDefinitions = messageDefinitions;
		this.listenerFactory = listenerFactory;
		this.cancellationToken = cancellationToken;
	}

	protected override void Connect(ServerSocket socket) {
		var logger = config.RuntimeLogger;
		var url = config.TcpUrl;

		logger.Information("Starting ZeroMQ server on {Url}...", url);
		socket.Bind(url);
		logger.Information("ZeroMQ server initialized, listening for connections on port {Port}.", config.Port);
	}

	protected override void Run(ServerSocket socket, MessageReplyTracker replyTracker, TaskManager taskManager) {
		var logger = config.RuntimeLogger;
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

				var connection = new RpcClientConnection<TOutgoingListener>(socket, routingId, messageDefinitions.Outgoing, replyTracker);
				connection.Closed += OnConnectionClosed;

				client = new Client(connection, messageDefinitions, listenerFactory(connection), logger, taskManager, cancellationToken);
				clients[routingId] = client;
			}

			LogMessageType(logger, routingId, data);
			messageDefinitions.Incoming.Handle(data, client);
		}

		foreach (var client in clients.Values) {
			client.Connection.Closed -= OnConnectionClosed;
		}
	}

	private void LogMessageType(ILogger logger, uint routingId, ReadOnlyMemory<byte> data) {
		if (!logger.IsEnabled(LogEventLevel.Verbose)) {
			return;
		}

		if (data.Length > 0 && messageDefinitions.Incoming.TryGetType(data, out var type)) {
			logger.Verbose("Received {MessageType} ({Bytes} B) from {RoutingId}.", type.Name, data.Length, routingId);
		}
		else {
			logger.Verbose("Received {Bytes} B message from {RoutingId}.", data.Length, routingId);
		}
	}

	private bool CheckIsRegistrationMessage(ReadOnlyMemory<byte> data, ILogger logger, uint routingId) {
		if (messageDefinitions.Incoming.TryGetType(data, out var type) && messageDefinitions.IsRegistrationMessage(type)) {
			return true;
		}

		logger.Warning("Received {MessageType} from {RoutingId} who is not registered.", type?.Name ?? "unknown message", routingId);
		return false;
	}
	
	private sealed class Client : MessageHandler<TIncomingListener> {
		public RpcClientConnection<TOutgoingListener> Connection { get; }
		
		private readonly IMessageDefinitions<TOutgoingListener, TIncomingListener, TReplyMessage> messageDefinitions;
		
		public Client(RpcClientConnection<TOutgoingListener> connection, IMessageDefinitions<TOutgoingListener, TIncomingListener, TReplyMessage> messageDefinitions, TIncomingListener listener, ILogger logger, TaskManager taskManager, CancellationToken cancellationToken) : base(listener, logger, taskManager, cancellationToken) {
			this.Connection = connection;
			this.messageDefinitions = messageDefinitions;
		}
	
		protected override Task SendReply(uint sequenceId, byte[] serializedReply) {
			return Connection.Send(messageDefinitions.CreateReplyMessage(sequenceId, serializedReply));
		}
	}
}
