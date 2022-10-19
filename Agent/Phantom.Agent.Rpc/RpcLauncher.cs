using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Data.Agent;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToServer;
using Phantom.Utils.Rpc;
using Phantom.Utils.Runtime;
using Serilog;
using Serilog.Events;

namespace Phantom.Agent.Rpc;

public sealed class RpcLauncher : RpcRuntime<ClientSocket> {
	public static async Task Launch(RpcConfiguration config, AgentAuthToken authToken, AgentInfo agentInfo, Func<ClientSocket, IMessageToAgentListener> listenerFactory, SemaphoreSlim disconnectSemaphore, CancellationToken receiveCancellationToken) {
		var socket = new ClientSocket();
		var options = socket.Options;

		options.CurveServerCertificate = config.ServerCertificate;
		options.CurveCertificate = new NetMQCertificate();
		options.HelloMessage = MessageRegistries.ToServer.Write(new RegisterAgentMessage(authToken, agentInfo)).ToArray();

		await new RpcLauncher(config, socket, agentInfo.Guid, listenerFactory, disconnectSemaphore, receiveCancellationToken).Launch();
	}

	private readonly RpcConfiguration config;
	private readonly Guid agentGuid;
	private readonly Func<ClientSocket, IMessageToAgentListener> messageListenerFactory;

	private readonly SemaphoreSlim disconnectSemaphore;
	private readonly CancellationToken receiveCancellationToken;

	private RpcLauncher(RpcConfiguration config, ClientSocket socket, Guid agentGuid, Func<ClientSocket, IMessageToAgentListener> messageListenerFactory, SemaphoreSlim disconnectSemaphore, CancellationToken receiveCancellationToken) : base(socket, config.Logger) {
		this.config = config;
		this.agentGuid = agentGuid;
		this.messageListenerFactory = messageListenerFactory;
		this.disconnectSemaphore = disconnectSemaphore;
		this.receiveCancellationToken = receiveCancellationToken;
	}

	protected override void Connect(ClientSocket socket) {
		var logger = config.Logger;
		var url = config.TcpUrl;

		logger.Information("Starting ZeroMQ client and connecting to {Url}...", url);
		socket.Connect(url);
		logger.Information("ZeroMQ client ready.");
	}

	protected override void Run(ClientSocket socket, TaskManager taskManager) {
		var logger = config.Logger;
		var listener = messageListenerFactory(socket);

		ServerMessaging.SetCurrentSocket(socket);
		var keepAliveLoop = new KeepAliveLoop(socket, taskManager);

		try {
			while (!receiveCancellationToken.IsCancellationRequested) {
				var data = socket.Receive(receiveCancellationToken);
				
				LogMessageType(logger, data);
				
				if (data.Length > 0) {
					MessageRegistries.ToAgent.Handle(data, listener, taskManager, receiveCancellationToken);
				}
			}
		} catch (OperationCanceledException) {
			// Ignore.
		} finally {
			logger.Verbose("ZeroMQ client stopped receiving messages.");

			disconnectSemaphore.Wait(CancellationToken.None);
			keepAliveLoop.Cancel();
		}
	}

	private static void LogMessageType(ILogger logger, ReadOnlyMemory<byte> data) {
		if (!logger.IsEnabled(LogEventLevel.Verbose)) {
			return;
		}

		if (data.Length > 0 && MessageRegistries.ToAgent.TryGetType(data, out var type)) {
			logger.Verbose("Received {MessageType} ({Bytes} B) from server.", type.Name, data.Length);
		}
		else {
			logger.Verbose("Received {Bytes} B message from server.", data.Length);
		}
	}

	protected override async Task Disconnect(ClientSocket socket) {
		var unregisterTimeoutTask = Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
		var finishedTask = await Task.WhenAny(socket.SendMessage(new UnregisterAgentMessage(agentGuid)), unregisterTimeoutTask);
		if (finishedTask == unregisterTimeoutTask) {
			config.Logger.Error("Timed out communicating agent shutdown with the server.");
		}
	}
}
