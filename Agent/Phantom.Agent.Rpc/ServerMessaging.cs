using NetMQ.Sockets;
using Phantom.Common.Logging;
using Phantom.Common.Messages;
using Serilog;

namespace Phantom.Agent.Rpc;

public static class ServerMessaging {
	private static readonly ILogger Logger = PhantomLogger.Create(typeof(ServerMessaging));
	
	private static ClientSocket? CurrentSocket { get; set; }
	private static readonly object SetCurrentSocketLock = new ();

	internal static void SetCurrentSocket(ClientSocket socket) {
		lock (SetCurrentSocketLock) {
			if (CurrentSocket != null) {
				throw new InvalidOperationException("Server socket can only be set once.");
			}
			
			CurrentSocket = socket;
		}
		
		Logger.Information("Server socket ready.");
	}

	public static async Task SendMessage<TMessage>(TMessage message) where TMessage : IMessageToServer {
		var currentSocket = CurrentSocket ?? throw new InvalidOperationException("Server socket not ready.");
		await currentSocket.SendMessage(message);
	}
}
