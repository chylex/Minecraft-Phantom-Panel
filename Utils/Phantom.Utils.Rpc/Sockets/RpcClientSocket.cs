using NetMQ;
using NetMQ.Sockets;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime;

namespace Phantom.Utils.Rpc.Sockets;

public static class RpcClientSocket {
	public static RpcClientSocket<TClientMessage, TServerMessage, TReplyMessage> Connect<TClientMessage, TServerMessage, TReplyMessage, THelloMessage>(RpcConfiguration config, IMessageDefinitions<TClientMessage, TServerMessage, TReplyMessage> messageDefinitions, THelloMessage helloMessage) where THelloMessage : TServerMessage where TReplyMessage : TClientMessage, TServerMessage {
		return RpcClientSocket<TClientMessage, TServerMessage, TReplyMessage>.Connect(config, messageDefinitions, helloMessage);
	}
}

public sealed class RpcClientSocket<TClientMessage, TServerMessage, TReplyMessage> : RpcSocket<ClientSocket> where TReplyMessage : TClientMessage, TServerMessage {
	internal static RpcClientSocket<TClientMessage, TServerMessage, TReplyMessage> Connect<THelloMessage>(RpcConfiguration config, IMessageDefinitions<TClientMessage, TServerMessage, TReplyMessage> messageDefinitions, THelloMessage helloMessage) where THelloMessage : TServerMessage {
		var socket = new ClientSocket();
		var options = socket.Options;

		options.CurveServerCertificate = config.ServerCertificate;
		options.CurveCertificate = new NetMQCertificate();
		options.HelloMessage = messageDefinitions.ToServer.Write(helloMessage).ToArray();
		RpcSocket.SetDefaultSocketOptions(options);

		var url = config.TcpUrl;
		var logger = PhantomLogger.Create(config.LoggerName);

		logger.Information("Starting ZeroMQ client and connecting to {Url}...", url);
		socket.Connect(url);
		logger.Information("ZeroMQ client ready.");

		return new RpcClientSocket<TClientMessage, TServerMessage, TReplyMessage>(socket, config, messageDefinitions);
	}

	public RpcConnectionToServer<TServerMessage> Connection { get; }
	internal IMessageDefinitions<TClientMessage, TServerMessage, TReplyMessage> MessageDefinitions { get; }

	private RpcClientSocket(ClientSocket socket, RpcConfiguration config, IMessageDefinitions<TClientMessage, TServerMessage, TReplyMessage> messageDefinitions) : base(socket, config) {
		MessageDefinitions = messageDefinitions;
		Connection = new RpcConnectionToServer<TServerMessage>(socket, messageDefinitions.ToServer, ReplyTracker);
	}
}
