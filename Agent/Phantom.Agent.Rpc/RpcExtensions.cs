using NetMQ;
using NetMQ.Sockets;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToServer;

namespace Phantom.Agent.Rpc; 

public static class RpcExtensions {
	public static async Task SendMessage<TMessage>(this ClientSocket socket, TMessage message) where TMessage : IMessageToServer {
		byte[] bytes = MessageRegistries.ToServer.Write(message).ToArray();
		if (bytes.Length > 0) {
			await socket.SendAsync(bytes);
		}
	}
	
	public static async Task SendMessageWithSequenceId<TMessage, TParam>(this ClientSocket socket, Func<uint, TParam, TMessage> messageFactory, TParam factoryParameter) where TMessage : IMessageToServer {
		byte[] bytes = MessageRegistries.ToServer.WriteWithSequenceId(messageFactory, factoryParameter).ToArray();
		if (bytes.Length > 0) {
			await socket.SendAsync(bytes);
		}
	}
	
	public static Task SendSimpleReply<TEnum>(this ClientSocket socket, TEnum reply) where TEnum : Enum {
		return SendMessageWithSequenceId(socket, SimpleReplyMessage.FromEnum, reply);
	}
}
