using NetMQ.Sockets;
using Phantom.Common.Logging;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToServer;
using Serilog;

namespace Phantom.Agent.Rpc;

public static class ServerMessaging {
	private static readonly ILogger Logger = PhantomLogger.Create(typeof(ServerMessaging));
	
	private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(10);

	private static ClientSocket? CurrentSocket { get; set; }
	private static readonly object SetCurrentSocketLock = new ();

	internal static void SetCurrentSocket(ClientSocket socket, CancellationToken cancellationToken) {
		Logger.Information("Server socket ready.");
		
		bool isFirstSet = false;
		lock (SetCurrentSocketLock) {
			if (CurrentSocket == null) {
				isFirstSet = true;
			}

			CurrentSocket = socket;
		}

		if (isFirstSet) {
			Task.Factory.StartNew(static o => SendKeepAliveLoop((CancellationToken) o!), cancellationToken, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}
	}

	public static async Task SendMessage<TMessage>(TMessage message) where TMessage : IMessageToServer {
		var currentSocket = CurrentSocket ?? throw new InvalidOperationException("Server socket not ready.");
		await currentSocket.SendMessage(message);
	}

	private static async Task SendKeepAliveLoop(CancellationToken cancellationToken) {
		try {
			while (true) {
				await Task.Delay(KeepAliveInterval, cancellationToken);

				var currentSocket = CurrentSocket;
				if (currentSocket != null) {
					await currentSocket.SendMessage(new AgentIsAliveMessage());
				}
			}
		} catch (OperationCanceledException) {
			// Ignore.
		} finally {
			Logger.Information("Stopped keep-alive loop.");
		}
	}
}
