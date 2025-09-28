using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Frame;
using Phantom.Utils.Rpc.Message;
using Serilog;

namespace Phantom.Utils.Rpc.Runtime.Server;

public sealed class RpcServerToClientConnection<TClientToServerMessage, TServerToClientMessage> {
	private readonly ILogger logger;
	private readonly RpcCommonConnectionParameters connectionParameters;
	private readonly MessageTypeMapping<TClientToServerMessage> messageTypeMapping;
	private readonly RpcServerClientSession<TServerToClientMessage> session;
	private readonly RpcStream stream;
	
	public Guid SessionId => session.SessionId;
	public MessageSender<TServerToClientMessage> MessageSender => session.MessageSender;
	
	internal RpcServerToClientConnection(
		RpcCommonConnectionParameters connectionParameters,
		MessageTypeMapping<TClientToServerMessage> messageTypeMapping,
		RpcServerClientSession<TServerToClientMessage> session,
		RpcStream stream
	) {
		this.logger = PhantomLogger.Create<RpcServerToClientConnection<TClientToServerMessage, TServerToClientMessage>>(session.LoggerName);
		this.connectionParameters = connectionParameters;
		this.messageTypeMapping = messageTypeMapping;
		this.session = session;
		this.stream = stream;
	}
	
	internal async Task Listen(IMessageReceiver<TClientToServerMessage> messageReceiver) {
		var messageHandler = new MessageHandler<TClientToServerMessage>(messageReceiver, session.FrameSender);
		var frameReader = new RpcFrameReader<TServerToClientMessage, TClientToServerMessage>(session.LoggerName, connectionParameters, messageTypeMapping, messageHandler, MessageSender, session.FrameSender);
		
		try {
			await IFrame.ReadFrom(stream, frameReader, session.CloseCancellationToken);
		} catch (OperationCanceledException) {
			return;
		}
		
		logger.Information("Client closed session.");
		await CloseSession();
	}
	
	public Task CloseSession() {
		return session.Close(closedByClient: true);
	}
}
