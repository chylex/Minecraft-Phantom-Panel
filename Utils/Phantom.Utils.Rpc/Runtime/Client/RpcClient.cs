using System.Net.Sockets;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Frame;
using Phantom.Utils.Rpc.Message;
using Serilog;

namespace Phantom.Utils.Rpc.Runtime.Client;

public sealed class RpcClient<TClientToServerMessage, TServerToClientMessage> : IRpcConnectionProvider, IDisposable {
	public static async Task<RpcClient<TClientToServerMessage, TServerToClientMessage>?> Connect(string loggerName, RpcClientConnectionParameters connectionParameters, IMessageDefinitions<TClientToServerMessage, TServerToClientMessage> messageDefinitions, CancellationToken cancellationToken) {
		RpcClientToServerConnector connector = new RpcClientToServerConnector(loggerName, connectionParameters);
		RpcClientToServerConnector.Connection? connection = await connector.ConnectWithRetries(maxAttempts: 10, cancellationToken);
		return connection == null ? null : new RpcClient<TClientToServerMessage, TServerToClientMessage>(loggerName, connectionParameters, connector, connection, messageDefinitions);
	}
	
	private readonly string loggerName;
	private readonly ILogger logger;
	
	private readonly RpcCommonConnectionParameters connectionParameters;
	private readonly RpcClientToServerConnector connector;
	private readonly IMessageDefinitions<TClientToServerMessage, TServerToClientMessage> messageDefinitions;
	private readonly IRpcFrameSenderProvider<TClientToServerMessage>.Mutable frameSenderProvider = new ();
	
	private RpcClientToServerConnector.Connection currentConnection;
	private readonly SemaphoreSlim currentConnectionSemaphore = new (1);
	
	private Task? listenerTask;
	
	private readonly CancellationTokenSource shutdownCancellationTokenSource = new ();
	
	public MessageSender<TClientToServerMessage> MessageSender { get; }
	
	private RpcClient(string loggerName, RpcCommonConnectionParameters connectionParameters, RpcClientToServerConnector connector, RpcClientToServerConnector.Connection connection, IMessageDefinitions<TClientToServerMessage, TServerToClientMessage> messageDefinitions) {
		this.loggerName = loggerName;
		this.logger = PhantomLogger.Create<RpcClient<TClientToServerMessage, TServerToClientMessage>>(loggerName);
		
		this.connectionParameters = connectionParameters;
		this.connector = connector;
		this.currentConnection = connection;
		this.messageDefinitions = messageDefinitions;
		
		this.MessageSender = new MessageSender<TClientToServerMessage>(loggerName, connectionParameters, frameSenderProvider);
	}
	
	async Task<RpcStream> IRpcConnectionProvider.GetStream(CancellationToken cancellationToken) {
		return (await GetConnection(cancellationToken)).Stream;
	}
	
	private async Task<RpcClientToServerConnector.Connection> GetConnection(CancellationToken cancellationToken) {
		await currentConnectionSemaphore.WaitAsync(cancellationToken);
		try {
			if (!currentConnection.Socket.Connected) {
				currentConnection = await connector.ConnectWithRetries(cancellationToken);
			}
			
			return currentConnection;
		} finally {
			currentConnectionSemaphore.Release();
		}
	}
	
	public void StartListening(IMessageReceiver<TServerToClientMessage> messageReceiver) {
		if (listenerTask != null) {
			throw new InvalidOperationException("Only one listener is allowed.");
		}
		
		listenerTask = Listen(messageReceiver);
	}
	
	private async Task Listen(IMessageReceiver<TServerToClientMessage> messageReceiver) {
		CancellationToken cancellationToken = shutdownCancellationTokenSource.Token;
		
		RpcClientToServerConnector.Connection? connection = null;
		SessionState? sessionState = null;
		
		try {
			while (true) {
				if (connection != null) {
					try {
						await connection.Shutdown();
					} catch (Exception e) {
						logger.Error(e, "Caught exception closing the socket.");
					}
					
					await connection.DisposeAsync();
					connection = null;
				}
				
				try {
					connection = await GetConnection(cancellationToken);
				} catch (OperationCanceledException) {
					throw;
				} catch (Exception e) {
					logger.Warning(e, "Could not obtain a new connection.");
					continue;
				}
				
				if (!sessionState.HasValue) {
					sessionState = NewSessionState(connection, messageReceiver);
				}
				else if (connection.IsNewSession) {
					await sessionState.Value.TryShutdownNow(logger);
					sessionState = NewSessionState(connection, messageReceiver);
				}
				else {
					sessionState.Value.Update(logger, connection);
				}
				
				try {
					await IFrame.ReadFrom(connection.Stream, sessionState.Value.FrameReader, cancellationToken);
				} catch (OperationCanceledException) {
					throw;
				} catch (EndOfStreamException) {
					logger.Warning("Socket was closed.");
					continue;
				} catch (SocketException e) {
					logger.Warning("Socket reading was interrupted. Socket error {ErrorCode} ({ErrorCodeName}), reason: {ErrorMessage}", e.ErrorCode, e.SocketErrorCode, e.Message);
					continue;
				} catch (Exception e) {
					logger.Error(e, "Socket reading was interrupted.");
					continue;
				}
				
				logger.Information("Server closed session.");
			}
		} finally {
			if (sessionState.HasValue) {
				await sessionState.Value.TryShutdown(logger, sendSessionTermination: cancellationToken.IsCancellationRequested);
			}
			
			if (connection != null) {
				try {
					await connection.Disconnect();
				} finally {
					await connection.DisposeAsync();
				}
			}
		}
	}
	
	private SessionState NewSessionState(RpcClientToServerConnector.Connection connection, IMessageReceiver<TServerToClientMessage> messageReceiver) {
		var frameSender = new RpcFrameSender<TClientToServerMessage>(loggerName, connectionParameters, this, messageDefinitions.ToServer, connection.PingInterval);
		var messageHandler = new MessageHandler<TServerToClientMessage>(messageReceiver, frameSender);
		var frameReader = new RpcFrameReader<TClientToServerMessage, TServerToClientMessage>(loggerName, connectionParameters, messageDefinitions.ToClient, messageHandler, MessageSender, frameSender);
		
		frameSenderProvider.SetNewValue(frameSender);
		messageReceiver.OnSessionRestarted();
		
		return new SessionState(frameSender, frameReader);
	}
	
	private readonly record struct SessionState(RpcFrameSender<TClientToServerMessage> FrameSender, RpcFrameReader<TClientToServerMessage, TServerToClientMessage> FrameReader) {
		public void Update(ILogger logger, RpcClientToServerConnector.Connection connection) {
			TimeSpan currentPingInterval = FrameSender.PingInterval;
			if (currentPingInterval != connection.PingInterval) {
				logger.Warning("Server requested a different ping interval ({ServerPingInterval}s) than currently set ({ClientPingInterval}s), but ping interval cannot be updated for existing sessions.", connection.PingInterval.TotalSeconds, currentPingInterval.TotalSeconds);
			}
		}
		
		public async Task TryShutdown(ILogger logger, bool sendSessionTermination) {
			try {
				await FrameSender.Shutdown(sendSessionTermination);
			} catch (Exception e) {
				logger.Error(e, "Caught exception while shutting down frame sender.");
			}
		}
		
		public async Task TryShutdownNow(ILogger logger) {
			try {
				await FrameSender.ShutdownNow();
			} catch (Exception e) {
				logger.Error(e, "Caught exception while immediately shutting down frame sender.");
			}
		}
	}
	
	public async Task Shutdown() {
		logger.Information("Shutting down client...");
		
		try {
			await MessageSender.Close();
		} catch (Exception e) {
			logger.Error(e, "Caught exception while closing message sender.");
		}
		
		await shutdownCancellationTokenSource.CancelAsync();
		
		if (listenerTask != null) {
			try {
				await listenerTask;
			} catch (OperationCanceledException) {
				// Ignore.
			} catch (Exception e) {
				logger.Error(e, "Caught exception in listener.");
			} finally {
				listenerTask = null;
			}
		}
		
		logger.Information("Client shut down.");
	}
	
	public void Dispose() {
		currentConnectionSemaphore.Dispose();
		shutdownCancellationTokenSource.Dispose();
	}
}
