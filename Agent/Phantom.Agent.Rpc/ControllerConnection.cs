﻿using Phantom.Common.Logging;
using Phantom.Common.Messages.Agent;
using Phantom.Utils.Rpc;
using Serilog;

namespace Phantom.Agent.Rpc;

public sealed class ControllerConnection {
	private static readonly ILogger Logger = PhantomLogger.Create(nameof(ControllerConnection));

	private readonly RpcConnectionToServer<IMessageToControllerListener> connection;
	
	public ControllerConnection(RpcConnectionToServer<IMessageToControllerListener> connection) {
		this.connection = connection;
		Logger.Information("Connection ready.");
	}

	public Task Send<TMessage>(TMessage message) where TMessage : IMessageToController {
		return connection.Send(message);
	}
}
