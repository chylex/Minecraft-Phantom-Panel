using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Data;
using Phantom.Common.Rpc;
using Phantom.Common.Rpc.Messages;
using Phantom.Common.Rpc.Messages.ToServer;

namespace Phantom.Agent.Rpc;

public sealed class RpcLauncher : RpcRuntime<ClientSocket> {
	public static async Task Launch(RpcConfiguration config, AgentAuthToken authToken, AgentInfo agentInfo) {
		var socket = new ClientSocket();
		var options = socket.Options;

		options.CurveServerCertificate = config.ServerCertificate;
		options.CurveCertificate = new NetMQCertificate();
		options.HelloMessage = MessageRegistries.ToServer.Write(new RegisterAgentMessage(authToken, agentInfo)).ToArray();
		
		await new RpcLauncher(config, socket).Launch();
	}

	private readonly RpcConfiguration config;

	private RpcLauncher(RpcConfiguration config, ClientSocket socket) : base(socket) {
		this.config = config;
	}

	protected override void Connect(ClientSocket socket) {
		var logger = config.Logger;
		var url = config.TcpUrl;

		logger.Information("Starting ZeroMQ client on {Url}...", url);
		socket.Connect(url);
		logger.Information("ZeroMQ client connected.");
	}

	protected override async Task Run(ClientSocket socket) {
		var cancellationToken = config.CancellationToken;
		var listener = new MessageListener(socket);

		// TODO optimize msg
		await foreach (var bytes in socket.ReceiveBytesAsyncEnumerable(cancellationToken)) {
			config.Logger.Verbose("Received {Bytes} B message from server.", bytes.Length);
			MessageRegistries.ToAgent.Handle(bytes, listener, cancellationToken);
		}
	}
}
