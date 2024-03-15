using Phantom.Common.Messages.Agent;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Runtime;
using Serilog;

namespace Phantom.Agent.Rpc;

public sealed class ControllerConnection {
	private static readonly ILogger Logger = PhantomLogger.Create(nameof(ControllerConnection));

	private readonly RpcConnectionToServer<IMessageToController> connection;
	
	public ControllerConnection(RpcConnectionToServer<IMessageToController> connection) {
		this.connection = connection;
		Logger.Information("Connection ready.");
	}

	public Task Send<TMessage>(TMessage message) where TMessage : IMessageToController {
		return connection.Send(message);
	}
}
