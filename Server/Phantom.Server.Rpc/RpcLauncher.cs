﻿using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Messages;
using Phantom.Utils.Rpc;
using Serilog.Events;

namespace Phantom.Server.Rpc;

public sealed class RpcLauncher : RpcRuntime<ServerSocket> {
	public static async Task Launch(RpcConfiguration config, Func<RpcClientConnection, IMessageToServerListener> listenerFactory) {
		var socket = new ServerSocket();
		var options = socket.Options;

		options.CurveServer = true;
		options.CurveCertificate = config.ServerCertificate;

		await new RpcLauncher(config, socket, listenerFactory).Launch();
	}

	private readonly RpcConfiguration config;
	private readonly Func<RpcClientConnection, IMessageToServerListener> listenerFactory;

	private RpcLauncher(RpcConfiguration config, ServerSocket socket, Func<RpcClientConnection, IMessageToServerListener> listenerFactory) : base(socket, config.CancellationToken) {
		this.config = config;
		this.listenerFactory = listenerFactory;
	}

	protected override void Connect(ServerSocket socket) {
		var logger = config.Logger;
		var url = config.TcpUrl;

		logger.Information("Starting ZeroMQ server on {Url}...", url);
		socket.Bind(url);
		logger.Information("ZeroMQ server initialized, listening for agent connections on port {Port}.", config.Port);
	}

	protected override async Task Run(ServerSocket socket, CancellationToken cancellationToken) {
		var logger = config.Logger;

		var clients = new Dictionary<ulong, Client>();

		void OnConnectionClosed(object? sender, RpcClientConnectionClosedEventArgs e) {
			clients.Remove(e.RoutingId);
			logger.Verbose("Closed connection to {RoutingId}.", e.RoutingId);
		}

		// TODO optimize msg
		await foreach (var (routingId, bytes) in socket.ReceiveBytesAsyncEnumerable(cancellationToken)) {
			if (logger.IsEnabled(LogEventLevel.Verbose)) {
				if (bytes.Length > 0 && MessageRegistries.ToServer.TryGetType(bytes, out var type)) {
					logger.Verbose("Received {MessageType} ({Bytes} B) from {RoutingId}.", type.Name, bytes.Length, routingId);
				}
				else {
					logger.Verbose("Received {Bytes} B message from {RoutingId}.", bytes.Length, routingId);
				}
			}

			if (bytes.Length == 0) {
				continue;
			}

			if (!clients.TryGetValue(routingId, out var client)) {
				var connection = new RpcClientConnection(socket, routingId);
				connection.Closed += OnConnectionClosed;
				
				client = new Client(connection, listenerFactory);
				clients[routingId] = client;
			}

			MessageRegistries.ToServer.Handle(bytes, client.Listener, cancellationToken);

			if (client.Listener.IsDisposed) {
				client.Connection.Close();
			}
		}
		
		foreach (var client in clients.Values) {
			client.Connection.Closed -= OnConnectionClosed;
		}
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