using Phantom.Common.Logging;
using Phantom.Common.Messages;
using Serilog;

namespace Phantom.Agent.Rpc;

public static class ServerMessaging {
	private static readonly ILogger Logger = PhantomLogger.Create(nameof(ServerMessaging));
	
	private static RpcServerConnection? CurrentConnection { get; set; }
	private static RpcServerConnection CurrentConnectionOrThrow => CurrentConnection ?? throw new InvalidOperationException("Server connection not ready.");
	
	private static readonly object SetCurrentConnectionLock = new ();

	internal static void SetCurrentConnection(RpcServerConnection connection) {
		lock (SetCurrentConnectionLock) {
			if (CurrentConnection != null) {
				throw new InvalidOperationException("Server connection can only be set once.");
			}
			
			CurrentConnection = connection;
		}
		
		Logger.Information("Server connection ready.");
	}

	public static Task Send<TMessage>(TMessage message) where TMessage : IMessageToServer {
		return CurrentConnectionOrThrow.Send(message);
	}

	public static Task<TReply?> Send<TMessage, TReply>(TMessage message, TimeSpan waitForReplyTime, CancellationToken waitForReplyCancellationToken) where TMessage : IMessageToServer<TReply> where TReply : class {
		return CurrentConnectionOrThrow.Send<TMessage, TReply>(message, waitForReplyTime, waitForReplyCancellationToken);
	}
}
