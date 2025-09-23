using System.Timers;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Message;
using Serilog;
using Timer = System.Timers.Timer;

namespace Phantom.Utils.Rpc.Runtime.Server;

sealed class RpcServerClientSession<TServerToClientMessage> : IRpcConnectionProvider {
	private static TimeSpan DisconnectedSessionTimeout => TimeSpan.FromHours(1);
	
	private readonly ILogger logger;
	private readonly RpcServerClientSessions<TServerToClientMessage> sessions;
	
	public string LoggerName { get; }
	public Guid SessionId { get; }
	public MessageSender<TServerToClientMessage> MessageSender { get; }
	public RpcFrameSender<TServerToClientMessage> FrameSender { get; }
	
	private bool isNew = true;
	public bool IsNew => isNew;
	
	private TaskCompletionSource<RpcStream> nextStream = new ();
	
	private readonly Timer closeAfterDisconnectionTimer;
	private readonly CancellationTokenSource closeCancellationTokenSource = new ();
	private bool isClosed = false;
	
	public CancellationToken CloseCancellationToken => closeCancellationTokenSource.Token;
	
	public RpcServerClientSession(string loggerName, RpcServerConnectionParameters connectionParameters, MessageRegistry<TServerToClientMessage> messageRegistry, RpcServerClientSessions<TServerToClientMessage> sessions, Guid sessionId) {
		this.logger = PhantomLogger.Create<RpcServerClientSession<TServerToClientMessage>>(loggerName);
		this.LoggerName = loggerName;
		this.sessions = sessions;
		this.SessionId = sessionId;
		this.FrameSender = new RpcFrameSender<TServerToClientMessage>(loggerName, connectionParameters, this, messageRegistry, connectionParameters.PingInterval);
		this.MessageSender = new MessageSender<TServerToClientMessage>(loggerName, connectionParameters, new IRpcFrameSenderProvider<TServerToClientMessage>.Constant(FrameSender));
		
		this.closeAfterDisconnectionTimer = new Timer(DisconnectedSessionTimeout) { AutoReset = false };
		this.closeAfterDisconnectionTimer.Elapsed += CloseAfterDisconnectionTimeout;
		this.closeAfterDisconnectionTimer.Start();
	}
	
	/// <returns>Whether this was a new session. If it was a new session, it will be marked as used.</returns>
	public bool MarkFirstTimeUse() {
		return Interlocked.CompareExchange(ref isNew, value: false, comparand: true);
	}
	
	public void OnConnected(RpcStream stream) {
		closeAfterDisconnectionTimer.Stop();
		
		lock (this) {
			if (!nextStream.Task.IsCanceled && !nextStream.TrySetResult(stream)) {
				nextStream = new TaskCompletionSource<RpcStream>();
				nextStream.SetResult(stream);
			}
		}
	}
	
	public void OnDisconnected() {
		lock (this) {
			var task = nextStream.Task;
			if (task is { IsCompleted: true, IsCanceled: false }) {
				nextStream = new TaskCompletionSource<RpcStream>();
			}
		}
		
		closeAfterDisconnectionTimer.Start();
	}
	
	Task<RpcStream> IRpcConnectionProvider.GetStream(CancellationToken cancellationToken) {
		Task<RpcStream> task;
		lock (this) {
			task = nextStream.Task;
		}
		
		return task.WaitAsync(cancellationToken);
	}
	
	public async Task Close(bool closedByClient) {
		logger.Information("Closing session...");
		await CloseImpl(closedByClient);
	}
	
	private void CloseAfterDisconnectionTimeout(object? sender, ElapsedEventArgs args) {
		logger.Information("Closing session due to timeout after disconnection...");
		_ = CloseImpl(closedByClient: false);
	}
	
	private async Task CloseImpl(bool closedByClient) {
		if (Interlocked.CompareExchange(ref isClosed, value: true, comparand: false)) {
			return;
		}
		
		sessions.Remove(this);
		closeAfterDisconnectionTimer.Close();
		
		await closeCancellationTokenSource.CancelAsync();
		
		lock (this) {
			if (!nextStream.TrySetCanceled()) {
				nextStream = new TaskCompletionSource<RpcStream>();
				nextStream.SetCanceled(CancellationToken.None);
			}
		}
		
		try {
			await MessageSender.Close();
		} catch (Exception e) {
			logger.Error(e, "Caught exception while closing message sender.");
		}
		
		try {
			await FrameSender.Shutdown(sendSessionTermination: !closedByClient);
		} catch (Exception e) {
			logger.Error(e, "Caught exception while closing send channel.");
		}
		
		logger.Information("Session closed.");
	}
}
