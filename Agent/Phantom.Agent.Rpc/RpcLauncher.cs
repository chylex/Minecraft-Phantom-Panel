using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Data.Agent;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToServer;
using Phantom.Utils.Rpc;
using Phantom.Utils.Runtime;
using Serilog.Events;

namespace Phantom.Agent.Rpc;

public sealed class RpcLauncher : RpcRuntime<ClientSocket> {
	public static async Task Launch(RpcConfiguration config, AgentAuthToken authToken, AgentInfo agentInfo, Func<ClientSocket, IMessageToAgentListener> listenerFactory) {
		var socket = new ClientSocket();
		var options = socket.Options;

		options.CurveServerCertificate = config.ServerCertificate;
		options.CurveCertificate = new NetMQCertificate();
		options.HelloMessage = MessageRegistries.ToServer.Write(new RegisterAgentMessage(authToken, agentInfo)).ToArray();
		
		await new RpcLauncher(config, socket, agentInfo.Guid, listenerFactory).Launch();
	}

	private readonly RpcConfiguration config;
	private readonly Guid agentGuid;
	private readonly Func<ClientSocket, IMessageToAgentListener> messageListenerFactory;

	private RpcLauncher(RpcConfiguration config, ClientSocket socket, Guid agentGuid, Func<ClientSocket, IMessageToAgentListener> messageListenerFactory) : base(socket, config.Logger) {
		this.config = config;
		this.agentGuid = agentGuid;
		this.messageListenerFactory = messageListenerFactory;
	}

	protected override void Connect(ClientSocket socket) {
		var logger = config.Logger;
		var url = config.TcpUrl;

		logger.Information("Starting ZeroMQ client and connecting to {Url}...", url);
		socket.Connect(url);
		logger.Information("ZeroMQ client ready.");
	}

	protected override async Task Run(ClientSocket socket, TaskManager taskManager) {
		var logger = config.Logger;
		var cancellationToken = config.CancellationToken;
		
		var listener = messageListenerFactory(socket);
		
		ServerMessaging.SetCurrentSocket(socket, cancellationToken);

		// TODO optimize msg
		await foreach (var bytes in socket.ReceiveBytesAsyncEnumerable(cancellationToken)) {
			if (logger.IsEnabled(LogEventLevel.Verbose)) {
				if (bytes.Length > 0 && MessageRegistries.ToAgent.TryGetType(bytes, out var type)) {
					logger.Verbose("Received {MessageType} ({Bytes} B) from server.", type.Name, bytes.Length);
				}
				else {
					logger.Verbose("Received {Bytes} B message from server.", bytes.Length);
				}
			}

			if (bytes.Length > 0) {
				MessageRegistries.ToAgent.Handle(bytes, listener, taskManager, cancellationToken);
			}
		}
	}

	protected override async Task Disconnect(ClientSocket socket) {
		var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
		var finishedTask = await Task.WhenAny(socket.SendMessage(new UnregisterAgentMessage(agentGuid)), timeoutTask);
		if (finishedTask == timeoutTask) {
			config.Logger.Error("Timed out communicating agent shutdown with the server.");
		}
	}
}
