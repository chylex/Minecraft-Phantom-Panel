using System.Collections.Concurrent;
using NetMQ.Sockets;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Sockets;
using Phantom.Utils.Tasks;
using Serilog.Events;

namespace Phantom.Utils.Rpc.Runtime;

public static class RpcServerRuntime {
	public static Task Launch<TClientListener, TServerListener, TReplyMessage>(RpcConfiguration config, IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions, Func<RpcConnectionToClient<TClientListener>, TServerListener> listenerFactory, CancellationToken cancellationToken) where TReplyMessage : IMessage<TClientListener, NoReply>, IMessage<TServerListener, NoReply> {
		return RpcServerRuntime<TClientListener, TServerListener, TReplyMessage>.Launch(config, messageDefinitions, listenerFactory, cancellationToken);
	}
}

internal sealed class RpcServerRuntime<TClientListener, TServerListener, TReplyMessage> : RpcRuntime<ServerSocket> where TReplyMessage : IMessage<TClientListener, NoReply>, IMessage<TServerListener, NoReply> {
	internal static Task Launch(RpcConfiguration config, IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions, Func<RpcConnectionToClient<TClientListener>, TServerListener> listenerFactory, CancellationToken cancellationToken) {
		var socket = RpcServerSocket.Connect(config);
		return new RpcServerRuntime<TClientListener, TServerListener, TReplyMessage>(socket, messageDefinitions, listenerFactory, cancellationToken).Launch();
	}

	private readonly IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions;
	private readonly Func<RpcConnectionToClient<TClientListener>, TServerListener> listenerFactory;
	private readonly TaskManager taskManager;
	private readonly CancellationToken cancellationToken;

	private RpcServerRuntime(RpcServerSocket socket, IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions, Func<RpcConnectionToClient<TClientListener>, TServerListener> listenerFactory, CancellationToken cancellationToken) : base(socket) {
		this.messageDefinitions = messageDefinitions;
		this.listenerFactory = listenerFactory;
		this.taskManager = new TaskManager(PhantomLogger.Create<TaskManager>(socket.Config.LoggerName + ":Runtime"));
		this.cancellationToken = cancellationToken;
	}

	private protected override Task Run(ServerSocket socket) {
		var clients = new ConcurrentDictionary<ulong, Client>();

		void OnConnectionClosed(object? sender, RpcClientConnectionClosedEventArgs e) {
			if (clients.Remove(e.RoutingId, out var client)) {
				client.Connection.Closed -= OnConnectionClosed;
			}
		}

		while (!cancellationToken.IsCancellationRequested) {
			var (routingId, data) = socket.Receive(cancellationToken);

			if (data.Length == 0) {
				LogUnknownMessage(routingId, data);
				continue;
			}

			Type? messageType = messageDefinitions.ToServer.TryGetType(data, out var type) ? type : null;
			if (messageType == null) {
				LogUnknownMessage(routingId, data);
				continue;
			}
			
			if (!clients.TryGetValue(routingId, out var client)) {
				if (!messageDefinitions.IsRegistrationMessage(messageType)) {
					RuntimeLogger.Warning("Received {MessageType} ({Bytes} B) from unregistered client {RoutingId}.", messageType.Name, data.Length, routingId);
					continue;
				}
				
				var clientLoggerName = LoggerName + ":" + routingId;
				var processingQueue = new RpcQueue(taskManager, "Process messages from " + routingId);
				var connection = new RpcConnectionToClient<TClientListener>(clientLoggerName, socket, routingId, messageDefinitions.ToClient, ReplyTracker);
				
				connection.Closed += OnConnectionClosed;

				client = new Client(clientLoggerName, connection, processingQueue, messageDefinitions, listenerFactory(connection), taskManager);
				clients[routingId] = client;
				client.EnqueueRegistrationMessage(messageType, data);
			}
			else {
				client.Enqueue(messageType, data);
			}
		}

		foreach (var client in clients.Values) {
			client.Connection.Close();
		}

		return taskManager.Stop();
	}

	private void LogUnknownMessage(uint routingId, ReadOnlyMemory<byte> data) {
		RuntimeLogger.Warning("Received unknown message ({Bytes} B) from {RoutingId}.", data.Length, routingId);
	}

	private protected override Task Disconnect(ServerSocket socket) {
		return Task.CompletedTask;
	}

	private sealed class Client : MessageHandler<TServerListener> {
		public RpcConnectionToClient<TClientListener> Connection { get; }
		
		private readonly RpcQueue processingQueue;
		private readonly IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions;
		private readonly TaskManager taskManager;
		
		public Client(string loggerName, RpcConnectionToClient<TClientListener> connection, RpcQueue processingQueue, IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions, TServerListener listener, TaskManager taskManager) : base(loggerName, listener) {
			this.Connection = connection;
			this.Connection.Closed += OnConnectionClosed;
			
			this.processingQueue = processingQueue;
			this.messageDefinitions = messageDefinitions;
			this.taskManager = taskManager;
		}

		internal void EnqueueRegistrationMessage(Type messageType, ReadOnlyMemory<byte> data) {
			LogMessageType(messageType, data);
			processingQueue.Enqueue(() => Handle(data));
		}
		
		internal void Enqueue(Type messageType, ReadOnlyMemory<byte> data) {
			LogMessageType(messageType, data);
			processingQueue.Enqueue(() => WaitForAuthorizationAndHandle(data));
		}

		private void LogMessageType(Type messageType, ReadOnlyMemory<byte> data) {
			if (Logger.IsEnabled(LogEventLevel.Verbose)) {
				Logger.Verbose("Received {MessageType} ({Bytes} B).", messageType.Name, data.Length);
			}
		}

		private void Handle(ReadOnlyMemory<byte> data) {
			messageDefinitions.ToServer.Handle(data, this);
		}

		private async Task WaitForAuthorizationAndHandle(ReadOnlyMemory<byte> data) {
			if (await Connection.GetAuthorization()) {
				Handle(data);
			}
			else {
				Logger.Warning("Dropped message after failed registration.");
			}
		}

		protected override Task SendReply(uint sequenceId, byte[] serializedReply) {
			return Connection.Send(messageDefinitions.CreateReplyMessage(sequenceId, serializedReply));
		}

		private void OnConnectionClosed(object? sender, RpcClientConnectionClosedEventArgs e) {
			Connection.Closed -= OnConnectionClosed;
			
			Logger.Debug("Closing connection...");
			
			taskManager.Run("Closing connection to " + e.RoutingId, async () => {
				await StopReceiving();
				await processingQueue.Stop();
				await Connection.StopSending();
				Logger.Debug("Connection closed.");
			});
		}
	}
}
