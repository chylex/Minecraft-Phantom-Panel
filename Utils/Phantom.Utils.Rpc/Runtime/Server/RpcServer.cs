using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Handshake;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime.Tls;
using Serilog;

namespace Phantom.Utils.Rpc.Runtime.Server;

public sealed class RpcServer<TClientToServerMessage, TServerToClientMessage> {
	private readonly string loggerName;
	private readonly ILogger logger;
	private readonly RpcServerConnectionParameters connectionParameters;
	private readonly MessageRegistries<TClientToServerMessage, TServerToClientMessage>.WithMapping messageRegistries;
	private readonly IRpcServerClientAuthProvider clientAuthProvider;
	private readonly IRpcServerClientHandshake clientHandshake;
	private readonly IRpcServerClientRegistrar<TClientToServerMessage, TServerToClientMessage> clientRegistrar;
	
	private readonly RpcServerClientSessions<TServerToClientMessage> clientSessions;
	private readonly List<Client> clients = [];
	
	public RpcServer(
		string loggerName,
		RpcServerConnectionParameters connectionParameters,
		MessageRegistries<TClientToServerMessage, TServerToClientMessage> messageRegistries,
		IRpcServerClientAuthProvider clientAuthProvider,
		IRpcServerClientHandshake clientHandshake,
		IRpcServerClientRegistrar<TClientToServerMessage, TServerToClientMessage> clientRegistrar
	) {
		this.loggerName = loggerName;
		this.logger = PhantomLogger.Create<RpcServer<TClientToServerMessage, TServerToClientMessage>>(loggerName);
		this.connectionParameters = connectionParameters;
		this.messageRegistries = messageRegistries.CreateMapping();
		this.clientAuthProvider = clientAuthProvider;
		this.clientHandshake = clientHandshake;
		this.clientRegistrar = clientRegistrar;
		this.clientSessions = new RpcServerClientSessions<TServerToClientMessage>(loggerName, connectionParameters, this.messageRegistries.ToClient.Mapping);
	}
	
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
			messageRegistries,
			clientAuthProvider,
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
		MessageRegistries<TClientToServerMessage, TServerToClientMessage>.WithMapping MessageDefinitions,
		IRpcServerClientAuthProvider ClientAuthProvider,
		IRpcServerClientHandshake ClientHandshake,
		IRpcServerClientRegistrar<TClientToServerMessage, TServerToClientMessage> ClientRegistrar,
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
			this.logger = PhantomLogger.Create<RpcServer<TClientToServerMessage, TServerToClientMessage>, Client>(PhantomLogger.ConcatNames(serverLoggerName, GetAddressDescriptor(socket)));
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
				var clientAuthToken = await stream.ReadAuthToken(cancellationToken);
				
				RpcAuthResult authResult = await CheckAuthorization(clientAuthToken);
				await stream.WriteByte(value: (byte) authResult, cancellationToken);
				await stream.Flush(cancellationToken);
				
				if (authResult != RpcAuthResult.Success) {
					return null;
				}
				
				var clientGuid = clientAuthToken.Guid;
				var sessionGuid = await stream.ReadGuid(cancellationToken);
				var session = await sharedData.ClientSessions.GetOrCreateSession(clientGuid, sessionGuid);
				
				RpcSessionRegistrationResult sessionRegistrationResult = session == null ? RpcSessionRegistrationResult.AlreadyClosed : RpcSessionRegistrationResult.Success;
				await stream.WriteByte(value: (byte) sessionRegistrationResult, cancellationToken);
				await stream.Flush(cancellationToken);
				
				if (session == null) {
					return null;
				}
				
				await stream.WriteUnsignedShort(sharedData.ConnectionParameters.PingIntervalSeconds, cancellationToken);
				await sharedData.MessageDefinitions.ToClient.Write(stream, cancellationToken);
				await sharedData.MessageDefinitions.ToServer.Write(stream, cancellationToken);
				await stream.Flush(cancellationToken);
				
				RpcFinalHandshakeResult finalHandshakeResult;
				
				var establishedConnection = await FinalizeHandshake(stream, clientGuid, session, cancellationToken);
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
		
		private async Task<RpcAuthResult> CheckAuthorization(AuthToken clientAuthToken) {
			var clientGuid = clientAuthToken.Guid;
			
			var expectedAuthSecret = await sharedData.ClientAuthProvider.GetAuthSecret(clientGuid);
			if (expectedAuthSecret == null) {
				logger.Warning("Rejected client, unknown client: {ClientGuid}", clientGuid);
				return RpcAuthResult.UnknownClient;
			}
			else if (!expectedAuthSecret.FixedTimeEquals(clientAuthToken.Secret)) {
				logger.Warning("Rejected client, invalid authorization secret.");
				return RpcAuthResult.InvalidSecret;
			}
			else {
				return RpcAuthResult.Success;
			}
		}
		
		private async Task<EstablishedConnection?> FinalizeHandshake(RpcStream stream, Guid clientGuid, RpcServerClientSession<TServerToClientMessage> session, CancellationToken cancellationToken) {
			logger.Information("Client {ClientGuid} connected with session {SessionGuid}, new logger name: {LoggerName}", clientGuid, session.SessionGuid, session.LoggerName);
			logger = PhantomLogger.Create<RpcServer<TClientToServerMessage, TServerToClientMessage>, Client>(session.LoggerName);
			
			try {
				await sharedData.ClientHandshake.Perform(session.IsNew, stream, clientGuid, cancellationToken);
			} catch (Exception e) {
				logger.Error(e, "Could not finish application handshake.");
				return null;
			}
			
			try {
				var connection = new RpcServerToClientConnection<TClientToServerMessage, TServerToClientMessage>(sharedData.ConnectionParameters, sharedData.MessageDefinitions.ToServer.Mapping, session, stream);
				var messageReceiver = sharedData.ClientRegistrar.Register(connection);
				
				return new EstablishedConnection(session, connection, messageReceiver);
			} catch (Exception e) {
				logger.Error(e, "Could not register client.");
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
