using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Phantom.Utils.Logging;
using Phantom.Utils.Monads;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime.Tls;
using Serilog;

namespace Phantom.Utils.Rpc.Runtime.Server;

public sealed class RpcServer<TClientToServerMessage, TServerToClientMessage, THandshakeResult>(
	string loggerName,
	RpcServerConnectionParameters connectionParameters,
	IMessageDefinitions<TClientToServerMessage, TServerToClientMessage> messageDefinitions,
	IRpcServerClientHandshake<THandshakeResult> clientHandshake,
	IRpcServerClientRegistrar<TClientToServerMessage, TServerToClientMessage, THandshakeResult> clientRegistrar
) {
	private readonly ILogger logger = PhantomLogger.Create<RpcServer<TClientToServerMessage, TServerToClientMessage, THandshakeResult>>(loggerName);
	private readonly RpcServerClientSessions<TServerToClientMessage> clientSessions = new (loggerName, connectionParameters, messageDefinitions.ToClient);
	private readonly List<Client> clients = [];
	
	public async Task<bool> Run(CancellationToken shutdownToken) {
		EndPoint endPoint = connectionParameters.EndPoint;
		
		var sslOptions = new SslServerAuthenticationOptions {
			AllowRenegotiation = false,
			AllowTlsResume = true,
			CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
			ClientCertificateRequired = false,
			EnabledSslProtocols = TlsSupport.SupportedProtocols,
			EncryptionPolicy = EncryptionPolicy.RequireEncryption,
			ServerCertificate = connectionParameters.Certificate.Certificate,
		};
		
		var serverData = new SharedData(
			connectionParameters,
			messageDefinitions.ToServer,
			clientHandshake,
			clientRegistrar,
			clientSessions
		);
		
		try {
			using var serverSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			
			try {
				serverSocket.Bind(endPoint);
				serverSocket.Listen(5);
			} catch (Exception e) {
				logger.Error(e, "Could not bind to {EndPoint}.", endPoint);
				return false;
			}
			
			try {
				logger.Information("Server listening on {EndPoint}.", endPoint);
				
				while (true) {
					Socket clientSocket = await serverSocket.AcceptAsync(shutdownToken);
					clients.Add(new Client(loggerName, serverData, clientSocket, sslOptions, shutdownToken));
					clients.RemoveAll(static client => client.Task.IsCompleted);
				}
			} catch (OperationCanceledException) {
				// Ignore.
			} finally {
				await Stop(serverSocket);
			}
		} catch (Exception e) {
			logger.Error(e, "Server crashed with uncaught exception.");
			return false;
		}
		
		return true;
	}
	
	private async Task Stop(Socket serverSocket) {
		logger.Information("Stopping server...");
		
		try {
			serverSocket.Close();
		} catch (Exception e) {
			logger.Error(e, "Server socket failed to close.");
			return;
		}
		
		logger.Information("Server socket closed, waiting for {RemainingClients} client session(s) to close...", clientSessions.Count);
		
		await clientSessions.CloseAll();
		await Task.WhenAll(clients.Select(static client => client.Task)).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
		
		logger.Information("Server stopped.");
	}
	
	private readonly record struct SharedData(
		RpcServerConnectionParameters ConnectionParameters,
		MessageRegistry<TClientToServerMessage> MessageRegistry,
		IRpcServerClientHandshake<THandshakeResult> ClientHandshake,
		IRpcServerClientRegistrar<TClientToServerMessage, TServerToClientMessage, THandshakeResult> ClientRegistrar,
		RpcServerClientSessions<TServerToClientMessage> ClientSessions
	);
	
	private sealed class Client {
		private static TimeSpan DisconnectTimeout => TimeSpan.FromSeconds(10);
		
		private static string GetAddressDescriptor(Socket socket) {
			EndPoint? endPoint = socket.RemoteEndPoint;
			
			return endPoint switch {
				IPEndPoint ip => ip.Port.ToString(),
				null          => "{unknown}",
				_             => "{" + endPoint + "}",
			};
		}
		
		public Task Task { get; }
		
		private ILogger logger;
		private readonly SharedData sharedData;
		private readonly Socket socket;
		private readonly SslServerAuthenticationOptions sslOptions;
		private readonly CancellationToken shutdownToken;
		
		public Client(
			string serverLoggerName,
			SharedData sharedData,
			Socket socket,
			SslServerAuthenticationOptions sslOptions,
			CancellationToken shutdownToken
		) {
			this.logger = PhantomLogger.Create<RpcServer<TClientToServerMessage, TServerToClientMessage, THandshakeResult>, Client>(PhantomLogger.ConcatNames(serverLoggerName, GetAddressDescriptor(socket)));
			this.sharedData = sharedData;
			this.socket = socket;
			this.sslOptions = sslOptions;
			this.shutdownToken = shutdownToken;
			
			this.Task = Run();
		}
		
		private async Task Run() {
			logger.Information("Accepted client.");
			try {
				await RunImpl();
			} catch (Exception e) {
				logger.Error(e, "Caught exception while processing client communication.");
			} finally {
				logger.Information("Disconnecting client socket...");
				try {
					using var timeoutTokenSource = new CancellationTokenSource(DisconnectTimeout);
					await socket.DisconnectAsync(reuseSocket: false, timeoutTokenSource.Token);
				} catch (OperationCanceledException) {
					logger.Warning("Could not disconnect client socket due to timeout.");
				} catch (SocketException e) {
					logger.Warning("Could not disconnect client socket. Socket error {ErrorCode} ({ErrorCodeName}), reason: {ErrorMessage}", e.ErrorCode, e.SocketErrorCode, e.Message);
				} catch (Exception e) {
					logger.Error(e, "Could not disconnect client socket.");
				} finally {
					socket.Close();
					logger.Information("Client socket closed.");
				}
			}
		}
		
		private async Task RunImpl() {
			await using var stream = new RpcStream(new SslStream(new NetworkStream(socket, ownsSocket: false), leaveInnerStreamOpen: false));
			
			EstablishedConnection? establishedConnection;
			using (var timeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
			using (var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken, timeoutCancellationTokenSource.Token)) {
				try {
					establishedConnection = await TryEstablishConnection(stream, combinedCancellationTokenSource.Token);
				} catch (OperationCanceledException) {
					// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
					if (timeoutCancellationTokenSource.IsCancellationRequested) {
						logger.Warning("Cancelling incoming client due to timeout.");
					}
					else {
						logger.Warning("Cancelling incoming client due to shutdown.");
					}
					
					return;
				}
			}
			
			if (establishedConnection == null) {
				return;
			}
			
			var (session, connection, messageReceiver) = establishedConnection;
			
			session.OnConnected(stream);
			try {
				await connection.Listen(messageReceiver);
			} catch (EndOfStreamException) {
				logger.Warning("Socket reading was interrupted, connection lost.");
			} catch (SocketException e) {
				logger.Warning("Socket reading was interrupted. Socket error {ErrorCode} ({ErrorCodeName}), reason: {ErrorMessage}", e.ErrorCode, e.SocketErrorCode, e.Message);
			} catch (Exception e) {
				logger.Error(e, "Socket reading was interrupted.");
			} finally {
				session.OnDisconnected();
			}
		}
		
		private async Task<EstablishedConnection?> TryEstablishConnection(RpcStream stream, CancellationToken cancellationToken) {
			try {
				await stream.AuthenticateAsServer(sslOptions, cancellationToken);
			} catch (OperationCanceledException) {
				throw;
			} catch (Exception e) {
				logger.Error(e, "Could not establish a secure connection.");
				return null;
			}
			
			try {
				var suppliedAuthToken = await stream.ReadAuthToken(cancellationToken);
				if (!sharedData.ConnectionParameters.AuthToken.FixedTimeEquals(suppliedAuthToken)) {
					logger.Warning("Rejected client, invalid authorization token.");
					await stream.WriteByte(value: 0, cancellationToken);
					await stream.Flush(cancellationToken);
					return null;
				}
				else {
					await stream.WriteByte(value: 1, cancellationToken);
					await stream.Flush(cancellationToken);
				}
				
				await stream.WriteUnsignedShort(sharedData.ConnectionParameters.PingIntervalSeconds, cancellationToken);
				await stream.Flush(cancellationToken);
				
				var sessionId = await stream.ReadGuid(cancellationToken);
				var session = sharedData.ClientSessions.GetOrCreateSession(sessionId);
				
				EstablishedConnection? establishedConnection = await FinalizeHandshake(stream, session, cancellationToken);
				RpcFinalHandshakeResult finalHandshakeResult;
				if (establishedConnection == null) {
					finalHandshakeResult = RpcFinalHandshakeResult.Error;
				}
				else {
					bool isNewSession = session.MarkFirstTimeUse();
					finalHandshakeResult = isNewSession ? RpcFinalHandshakeResult.NewSession : RpcFinalHandshakeResult.ReusedSession;
				}
				
				await stream.WriteByte((byte) finalHandshakeResult, cancellationToken);
				await stream.Flush(cancellationToken);
				
				return establishedConnection;
			} catch (OperationCanceledException) {
				throw;
			} catch (EndOfStreamException) {
				logger.Warning("Could not perform application handshake, connection lost.");
				return null;
			} catch (Exception e) {
				logger.Warning(e, "Could not perform application handshake.");
				return null;
			}
		}
		
		private async Task<EstablishedConnection?> FinalizeHandshake(RpcStream stream, RpcServerClientSession<TServerToClientMessage> session, CancellationToken cancellationToken) {
			logger.Information("Client connected with session {SessionId}, new logger name: {LoggerName}", session.SessionId, session.LoggerName);
			logger = PhantomLogger.Create<RpcServer<TClientToServerMessage, TServerToClientMessage, THandshakeResult>, Client>(session.LoggerName);
			
			switch (await sharedData.ClientHandshake.Perform(session.IsNew, stream, cancellationToken)) {
				case Left<THandshakeResult, Exception>(var handshakeResult):
					try {
						var connection = new RpcServerToClientConnection<TClientToServerMessage, TServerToClientMessage>(sharedData.ConnectionParameters, sharedData.MessageRegistry, session, stream);
						var messageReceiver = sharedData.ClientRegistrar.Register(connection, handshakeResult);
						
						return new EstablishedConnection(session, connection, messageReceiver);
					} catch (Exception e) {
						logger.Error(e, "Could not register client.");
						return null;
					}
				
				case Right<THandshakeResult, Exception>(var exception):
					logger.Error(exception, "Could not finish application handshake.");
					return null;
				
				default:
					return null;
			}
		}
		
		private sealed record EstablishedConnection(
			RpcServerClientSession<TServerToClientMessage> Session,
			RpcServerToClientConnection<TClientToServerMessage, TServerToClientMessage> Connection,
			IMessageReceiver<TClientToServerMessage> MessageReceiver
		);
	}
}
