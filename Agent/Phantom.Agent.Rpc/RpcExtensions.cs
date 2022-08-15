using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Rpc.Message;
using Phantom.Common.Rpc.Messages;

namespace Phantom.Agent.Rpc; 

public static class RpcExtensions {
	public static async Task SendMessage<TMessage>(this ClientSocket socket, TMessage message) where TMessage : IMessageToServer {
		byte[] bytes = MessageRegistries.ToServer.Write(message).ToArray();
		if (bytes.Length > 0) {
			await socket.SendAsync(bytes);
		}
	}
}
