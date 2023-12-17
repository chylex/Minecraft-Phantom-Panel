using NetMQ;
using NetMQ.Sockets;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime;

namespace Phantom.Utils.Rpc.Sockets;

public static class RpcClientSocket {
	public static RpcClientSocket<TClientListener, TServerListener, TReplyMessage> Connect<TClientListener, TServerListener, TReplyMessage, THelloMessage>(RpcConfiguration config, IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions, THelloMessage helloMessage) where THelloMessage : IMessage<TServerListener, NoReply> where TReplyMessage : IMessage<TClientListener, NoReply>, IMessage<TServerListener, NoReply> {
		return RpcClientSocket<TClientListener, TServerListener, TReplyMessage>.Connect(config, messageDefinitions, helloMessage);
	}
}

public sealed class RpcClientSocket<TClientListener, TServerListener, TReplyMessage> : RpcSocket<ClientSocket> where TReplyMessage : IMessage<TClientListener, NoReply>, IMessage<TServerListener, NoReply> {
	internal static RpcClientSocket<TClientListener, TServerListener, TReplyMessage> Connect<THelloMessage>(RpcConfiguration config, IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions, THelloMessage helloMessage) where THelloMessage : IMessage<TServerListener, NoReply> {
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

		return new RpcClientSocket<TClientListener, TServerListener, TReplyMessage>(socket, config, messageDefinitions);
	}

	public RpcConnectionToServer<TServerListener> Connection { get; }
	internal IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> MessageDefinitions { get; }

	private RpcClientSocket(ClientSocket socket, RpcConfiguration config, IMessageDefinitions<TClientListener, TServerListener, TReplyMessage> messageDefinitions) : base(socket, config) {
		MessageDefinitions = messageDefinitions;
		Connection = new RpcConnectionToServer<TServerListener>(config.LoggerName, socket, messageDefinitions.ToServer, ReplyTracker);
	}
}
