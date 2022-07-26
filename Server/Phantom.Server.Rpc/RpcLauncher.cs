﻿using NetMQ.Sockets;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToServer;
using Phantom.Utils.Rpc;
using Phantom.Utils.Runtime;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace Phantom.Server.Rpc;

public sealed class RpcLauncher : RpcRuntime<ServerSocket> {
	public static async Task Launch(RpcConfiguration config, Func<RpcClientConnection, IMessageToServerListener> listenerFactory, CancellationToken cancellationToken) {
		var socket = new ServerSocket();
		var options = socket.Options;

		options.CurveServer = true;
		options.CurveCertificate = config.ServerCertificate;

		await new RpcLauncher(config, socket, listenerFactory, cancellationToken).Launch();
	}

	private readonly RpcConfiguration config;
	private readonly Func<RpcClientConnection, IMessageToServerListener> listenerFactory;
	private readonly CancellationToken cancellationToken;

	private RpcLauncher(RpcConfiguration config, ServerSocket socket, Func<RpcClientConnection, IMessageToServerListener> listenerFactory, CancellationToken cancellationToken) : base(socket, config.Logger) {
		this.config = config;
		this.listenerFactory = listenerFactory;
		this.cancellationToken = cancellationToken;
	}

	protected override void Connect(ServerSocket socket) {
		var logger = config.Logger;
		var url = config.TcpUrl;

		logger.Information("Starting ZeroMQ server on {Url}...", url);
		socket.Bind(url);
		logger.Information("ZeroMQ server initialized, listening for agent connections on port {Port}.", config.Port);
	}

	protected override void Run(ServerSocket socket, TaskManager taskManager) {
		var logger = config.Logger;
		var clients = new Dictionary<ulong, Client>();

		void OnConnectionClosed(object? sender, RpcClientConnectionClosedEventArgs e) {
			clients.Remove(e.RoutingId);
			logger.Verbose("Closed connection to {RoutingId}.", e.RoutingId);
		}

		while (!cancellationToken.IsCancellationRequested) {
			var (routingId, data) = socket.Receive(cancellationToken);

			if (data.Length == 0) {
				LogMessageType(logger, routingId, data);
				continue;
			}

			if (!clients.TryGetValue(routingId, out var client)) {
				if (!CheckIsAgentRegistrationMessage(data, logger, routingId)) {
					continue;
				}

				var connection = new RpcClientConnection(socket, routingId);
				connection.Closed += OnConnectionClosed;

				client = new Client(connection, listenerFactory);
				clients[routingId] = client;
			}

			LogMessageType(logger, routingId, data);
			MessageRegistries.ToServer.Handle(data, client.Listener, taskManager, cancellationToken);

			if (client.Listener.IsDisposed) {
				client.Connection.Close();
			}
		}

		foreach (var client in clients.Values) {
			client.Connection.Closed -= OnConnectionClosed;
		}
	}

	private static void LogMessageType(ILogger logger, uint routingId, ReadOnlyMemory<byte> data) {
		if (!logger.IsEnabled(LogEventLevel.Verbose)) {
			return;
		}

		if (data.Length > 0 && MessageRegistries.ToServer.TryGetType(data, out var type)) {
			logger.Verbose("Received {MessageType} ({Bytes} B) from {RoutingId}.", type.Name, data.Length, routingId);
		}
		else {
			logger.Verbose("Received {Bytes} B message from {RoutingId}.", data.Length, routingId);
		}
	}

	private static bool CheckIsAgentRegistrationMessage(ReadOnlyMemory<byte> data, ILogger logger, uint routingId) {
		if (MessageRegistries.ToServer.TryGetType(data, out var type) && type == typeof(RegisterAgentMessage)) {
			return true;
		}

		logger.Warning("Received {MessageType} from a non-registered agent {RoutingId}.", type?.Name ?? "unknown message", routingId);
		return false;
	}

	private readonly struct Client {
		public RpcClientConnection Connection { get; }
		public IMessageToServerListener Listener { get; }

		public Client(RpcClientConnection connection, Func<RpcClientConnection, IMessageToServerListener> listenerFactory) {
			Connection = connection;
			Listener = listenerFactory(connection);
		}
	}
}
