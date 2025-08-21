using System.Collections.Concurrent;
using Akka.Actor;
using NetMQ.Sockets;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Sockets;
using Serilog;
using Serilog.Events;

namespace Phantom.Utils.Rpc.Runtime;

public static class RpcServerRuntime {
	public static Task Launch<TClientMessage, TServerMessage, TRegistrationMessage, TReplyMessage>(
		RpcConfiguration config,
		IMessageDefinitions<TClientMessage, TServerMessage, TReplyMessage> messageDefinitions,
		IRegistrationHandler<TClientMessage, TServerMessage, TRegistrationMessage> registrationHandler,
		IActorRefFactory actorSystem,
		CancellationToken cancellationToken
	) where TRegistrationMessage : TServerMessage where TReplyMessage : TClientMessage, TServerMessage {
		return RpcServerRuntime<TClientMessage, TServerMessage, TRegistrationMessage, TReplyMessage>.Launch(config, messageDefinitions, registrationHandler, actorSystem, cancellationToken);
	}
}

internal sealed class RpcServerRuntime<TClientMessage, TServerMessage, TRegistrationMessage, TReplyMessage> : RpcRuntime<ServerSocket> where TRegistrationMessage : TServerMessage where TReplyMessage : TClientMessage, TServerMessage {
	internal static Task Launch(RpcConfiguration config, IMessageDefinitions<TClientMessage, TServerMessage, TReplyMessage> messageDefinitions, IRegistrationHandler<TClientMessage, TServerMessage, TRegistrationMessage> registrationHandler, IActorRefFactory actorSystem, CancellationToken cancellationToken) {
		var socket = RpcServerSocket.Connect(config);
		return new RpcServerRuntime<TClientMessage, TServerMessage, TRegistrationMessage, TReplyMessage>(socket, messageDefinitions, registrationHandler, actorSystem, cancellationToken).Launch();
	}
	
	private readonly string serviceName;
	private readonly IMessageDefinitions<TClientMessage, TServerMessage, TReplyMessage> messageDefinitions;
	private readonly IRegistrationHandler<TClientMessage, TServerMessage, TRegistrationMessage> registrationHandler;
	private readonly IActorRefFactory actorSystem;
	private readonly CancellationToken cancellationToken;
	
	private RpcServerRuntime(RpcServerSocket socket, IMessageDefinitions<TClientMessage, TServerMessage, TReplyMessage> messageDefinitions, IRegistrationHandler<TClientMessage, TServerMessage, TRegistrationMessage> registrationHandler, IActorRefFactory actorSystem, CancellationToken cancellationToken) : base(socket) {
		this.serviceName = socket.Config.ServiceName;
		this.messageDefinitions = messageDefinitions;
		this.registrationHandler = registrationHandler;
		this.actorSystem = actorSystem;
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
				if (messageType != typeof(TRegistrationMessage)) {
					RuntimeLogger.Warning("Received {MessageType} ({Bytes} B) from unregistered client {RoutingId}.", messageType.Name, data.Length, routingId);
					continue;
				}
				
				var clientLoggerName = LoggerName + ":" + routingId;
				var clientActorName = "Rpc-" + serviceName + "-" + routingId;
				
				// TODO add pings and tear down connection after too much inactivity
				var connection = new RpcConnectionToClient<TClientMessage>(socket, routingId, messageDefinitions.ToClient, ReplyTracker);
				connection.Closed += OnConnectionClosed;
				
				client = new Client(clientLoggerName, clientActorName, connection, actorSystem, messageDefinitions, registrationHandler);
				clients[routingId] = client;
			}
			
			client.Enqueue(messageType, data);
		}
		
		foreach (var client in clients.Values) {
			client.Connection.Close();
		}
		
		return Task.CompletedTask;
	}
	
	private void LogUnknownMessage(uint routingId, ReadOnlyMemory<byte> data) {
		RuntimeLogger.Warning("Received unknown message ({Bytes} B) from {RoutingId}.", data.Length, routingId);
	}
	
	private protected override Task Disconnect(ServerSocket socket) {
		return Task.CompletedTask;
	}
	
	private sealed class Client {
		public RpcConnectionToClient<TClientMessage> Connection { get; }
		
		private readonly ILogger logger;
		private readonly ActorRef<RpcReceiverActor<TClientMessage, TServerMessage, TRegistrationMessage, TReplyMessage>.ReceiveMessageCommand> receiverActor;
		
		public Client(string loggerName, string actorName, RpcConnectionToClient<TClientMessage> connection, IActorRefFactory actorSystem, IMessageDefinitions<TClientMessage, TServerMessage, TReplyMessage> messageDefinitions, IRegistrationHandler<TClientMessage, TServerMessage, TRegistrationMessage> registrationHandler) {
			this.Connection = connection;
			this.Connection.Closed += OnConnectionClosed;
			
			this.logger = PhantomLogger.Create(loggerName);
			
			var receiverActorInit = new RpcReceiverActor<TClientMessage, TServerMessage, TRegistrationMessage, TReplyMessage>.Init(loggerName, messageDefinitions, registrationHandler, Connection);
			this.receiverActor = actorSystem.ActorOf(RpcReceiverActor<TClientMessage, TServerMessage, TRegistrationMessage, TReplyMessage>.Factory(receiverActorInit), actorName + "-Receiver");
		}
		
		internal void Enqueue(Type messageType, ReadOnlyMemory<byte> data) {
			LogMessageType(messageType, data);
			receiverActor.Tell(new RpcReceiverActor<TClientMessage, TServerMessage, TRegistrationMessage, TReplyMessage>.ReceiveMessageCommand(messageType, data));
		}
		
		private void LogMessageType(Type messageType, ReadOnlyMemory<byte> data) {
			if (logger.IsEnabled(LogEventLevel.Verbose)) {
				logger.Verbose("Received {MessageType} ({Bytes} B).", messageType.Name, data.Length);
			}
		}
		
		private void OnConnectionClosed(object? sender, RpcClientConnectionClosedEventArgs e) {
			Connection.Closed -= OnConnectionClosed;
			
			logger.Debug("Closing connection...");
			receiverActor.Stop();
		}
	}
}
