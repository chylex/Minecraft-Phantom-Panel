using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Rpc;
using Phantom.Common.Rpc.Messages;
using Phantom.Common.Rpc.Messages.ToServer;

namespace Phantom.Agent.Rpc;

public sealed class RpcLauncher : RpcRuntime<ClientSocket> {
	public static async Task Launch(RpcConfiguration config) {
		await new RpcLauncher(config).Launch();
	}

	private readonly RpcConfiguration config;

	private RpcLauncher(RpcConfiguration config) {
		this.config = config;
	}

	protected override void Connect(ClientSocket socket) {
		var logger = config.Logger;
		var url = config.TcpUrl;

		logger.Information("Starting ZeroMQ client on {Url}...", url);
		
		socket.Options.HelloMessage = MessageRegistries.ToServer.Write(new AgentAuthenticationMessage {
			AgentGuid = Guid.NewGuid(),
			AgentVersion = 1,
			AuthToken = "test"
		}).ToArray();
		
		socket.Connect(url);
		logger.Information("ZeroMQ client connected.");

		socket.Send("test");
	}

	protected override async Task Run(ClientSocket socket) {
		await foreach (var data in socket.ReceiveStringAsyncEnumerable(config.CancellationToken)) {
			// TODO
			config.Logger.Verbose("Received message from server: {data}", data);
		}
	}
}
