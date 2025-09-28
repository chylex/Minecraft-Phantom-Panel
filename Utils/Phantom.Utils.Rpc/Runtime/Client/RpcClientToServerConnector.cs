using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Phantom.Utils.Collections;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime.Tls;
using Serilog;
using Serilog.Events;

namespace Phantom.Utils.Rpc.Runtime.Client;

[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
sealed class RpcClientToServerConnector<TClientToServerMessage, TServerToClientMessage> {
	private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromMilliseconds(500);
	private static readonly TimeSpan MaximumRetryDelay = TimeSpan.FromSeconds(30);
	private static readonly TimeSpan DisconnectTimeout = TimeSpan.FromSeconds(10);
	
	private readonly ILogger logger;
	private readonly RpcClientConnectionParameters parameters;
	private readonly MessageRegistries<TClientToServerMessage, TServerToClientMessage> messageRegistries;
	private readonly Guid sessionId;
	private readonly SslClientAuthenticationOptions sslOptions;
	
	private bool loggedCertificateValidationError = false;
	
	public RpcClientToServerConnector(string loggerName, RpcClientConnectionParameters parameters, MessageRegistries<TClientToServerMessage, TServerToClientMessage> messageRegistries) {
		this.logger = PhantomLogger.Create<RpcClientToServerConnector<TClientToServerMessage, TServerToClientMessage>>(loggerName);
		this.parameters = parameters;
		this.messageRegistries = messageRegistries;
		this.sessionId = Guid.NewGuid();
		
		this.sslOptions = new SslClientAuthenticationOptions {
			AllowRenegotiation = false,
			AllowTlsResume = true,
			CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
			EnabledSslProtocols = TlsSupport.SupportedProtocols,
			EncryptionPolicy = EncryptionPolicy.RequireEncryption,
			RemoteCertificateValidationCallback = ValidateServerCertificate,
			TargetHost = parameters.DistinguishedName,
		};
	}
	
	internal async Task<Connection?> ConnectWithRetries(int maxAttempts, CancellationToken cancellationToken) {
		logger.Information("Connecting to {Host}:{Port}...", parameters.Host, parameters.Port);
		
		int attempt = 1;
		TimeSpan nextAttemptDelay = InitialRetryDelay;
		
		while (true) {
			var newConnection = await TryConnect(cancellationToken);
			if (newConnection != null) {
				return newConnection;
			}
			
			cancellationToken.ThrowIfCancellationRequested();
			
			if (attempt >= maxAttempts) {
				break;
			}
			
			logger.Warning("Attempt {Attempt} out of {MaxAttempts} failed. Retrying in {Seconds}s.", attempt, maxAttempts, nextAttemptDelay.TotalSeconds.ToString("F1"));
			nextAttemptDelay = await WaitForRetry(nextAttemptDelay, cancellationToken);
			attempt++;
		}
		
		logger.Error("Attempt {Attempt} out of {MaxAttempts} failed.", attempt, maxAttempts);
		return null;
	}
	
	internal async Task<Connection> ConnectWithRetries(CancellationToken cancellationToken) {
		logger.Information("Connecting to {Host}:{Port}...", parameters.Host, parameters.Port);
		
		TimeSpan nextAttemptDelay = InitialRetryDelay;
		
		while (true) {
			var newConnection = await TryConnect(cancellationToken);
			if (newConnection != null) {
				return newConnection;
			}
			
			cancellationToken.ThrowIfCancellationRequested();
			
			logger.Warning("Retrying in {Seconds}s.", nextAttemptDelay.TotalSeconds.ToString("F1"));
			nextAttemptDelay = await WaitForRetry(nextAttemptDelay, cancellationToken);
		}
	}
	
	private static async Task<TimeSpan> WaitForRetry(TimeSpan nextAttemptDelay, CancellationToken cancellationToken) {
		await Task.Delay(nextAttemptDelay, cancellationToken);
		return Comparables.Min(nextAttemptDelay.Multiply(1.5), MaximumRetryDelay);
	}
	
	private async Task<Connection?> TryConnect(CancellationToken cancellationToken) {
		try {
			return await TryConnectImpl(cancellationToken);
		} catch (Exception e) {
			logger.Error(e, "Caught unhandled exception while connecting.");
			return null;
		}
	}
	
	private async Task<Connection?> TryConnectImpl(CancellationToken cancellationToken) {
		Socket clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
		try {
			await clientSocket.ConnectAsync(parameters.Host, parameters.Port, cancellationToken);
		} catch (SocketException e) {
			logger.Warning("Could not connect. Socket error {ErrorCode} ({ErrorCodeName}), reason: {ErrorMessage}", e.ErrorCode, e.SocketErrorCode, e.Message);
			return null;
		} catch (Exception e) {
			logger.Warning(e, "Could not connect.");
			return null;
		}
		
		RpcStream? stream;
		try {
			stream = new RpcStream(new SslStream(new NetworkStream(clientSocket, ownsSocket: false), leaveInnerStreamOpen: false));
			
			if (await AuthenticateAndPerformHandshake(stream, cancellationToken) is {} result) {
				logger.Information("Connected to {Host}:{Port}.", parameters.Host, parameters.Port);
				return new Connection(clientSocket, stream, result.IsNewSession, result.PingInterval, result.MessageTypeMappings);
			}
		} catch (Exception e) {
			logger.Error(e, "Caught unhandled exception.");
			stream = null;
		}
		
		try {
			await DisconnectSocket(clientSocket, stream);
		} finally {
			clientSocket.Close();
		}
		
		return null;
	}
	
	private async Task<ConnectionResult?> AuthenticateAndPerformHandshake(RpcStream stream, CancellationToken cancellationToken) {
		try {
			loggedCertificateValidationError = false;
			await stream.AuthenticateAsClient(sslOptions, cancellationToken);
		} catch (AuthenticationException e) {
			if (!loggedCertificateValidationError) {
				logger.Error(e, "Could not establish a secure connection.");
			}
			
			return null;
		}
		
		logger.Information("Established a secure connection.");
		
		try {
			return await PerformApplicationHandshake(stream, cancellationToken);
		} catch (EndOfStreamException) {
			logger.Warning("Could not perform application handshake, connection lost.");
			return null;
		} catch (Exception e) {
			logger.Warning(e, "Could not perform application handshake.");
			return null;
		}
	}
	
	private async Task<ConnectionResult?> PerformApplicationHandshake(RpcStream stream, CancellationToken cancellationToken) {
		await stream.WriteAuthToken(parameters.AuthToken, cancellationToken);
		await stream.Flush(cancellationToken);
		
		if (await stream.ReadByte(cancellationToken) != 1) {
			logger.Error("Server rejected authorization token.");
			return null;
		}
		
		await stream.WriteGuid(sessionId, cancellationToken);
		await stream.Flush(cancellationToken);
		
		var pingInterval = await ReadPingInterval(stream, cancellationToken);
		if (pingInterval == null) {
			return null;
		}
		
		var mappedMessageDefinitions = await ReadMessageMappings(stream, cancellationToken);
		
		await parameters.Handshake.Perform(stream, cancellationToken);
		
		var finalHandshakeResult = (RpcFinalHandshakeResult) await stream.ReadByte(cancellationToken);
		if (finalHandshakeResult == RpcFinalHandshakeResult.Error) {
			logger.Error("Server rejected client due to unknown error.");
			return null;
		}
		
		return new ConnectionResult(finalHandshakeResult == RpcFinalHandshakeResult.NewSession, pingInterval.Value, mappedMessageDefinitions);
	}
	
	private async Task<TimeSpan?> ReadPingInterval(RpcStream stream, CancellationToken cancellationToken) {
		ushort pingIntervalSeconds = await stream.ReadUnsignedShort(cancellationToken);
		if (pingIntervalSeconds == 0) {
			logger.Error("Server sent invalid ping interval.");
			return null;
		}
		
		logger.Debug("Server requested a ping interval of {PingInterval}s.", pingIntervalSeconds);
		return TimeSpan.FromSeconds(pingIntervalSeconds);
	}
	
	private async Task<MessageTypeMappings<TClientToServerMessage, TServerToClientMessage>> ReadMessageMappings(RpcStream stream, CancellationToken cancellationToken) {
		var toClient = await ReadMessageMapping(messageRegistries.ToClient, stream, cancellationToken);
		var toServer = await ReadMessageMapping(messageRegistries.ToServer, stream, cancellationToken);
		
		return new MessageTypeMappings<TClientToServerMessage, TServerToClientMessage>(toClient, toServer);
	}
	
	private async Task<MessageTypeMapping<TMessageBase>> ReadMessageMapping<TMessageBase>(MessageRegistry<TMessageBase> messageRegistry, RpcStream stream, CancellationToken cancellationToken) {
		var result = await messageRegistry.ReadMapping(stream, cancellationToken);
		
		if (logger.IsEnabled(LogEventLevel.Debug)) {
			foreach ((byte messageTypeCode, MessageTypeName messageTypeName) in result.SupportedMessages) {
				logger.Debug("Server requested code {MessageCode} for message {MessageBaseTypeName}:{MessageTypeName}.", messageTypeCode, typeof(TMessageBase).Name, messageTypeName);
			}
		}
		
		foreach ((byte messageTypeCode, MessageTypeName messageTypeName) in result.UnsupportedMessages) {
			logger.Warning("Server requested code {MessageCode} for message {MessageBaseTypeName}:{MessageTypeName} that the client does not support.", messageTypeCode, typeof(TMessageBase).Name, messageTypeName);
		}
		
		return result.TypeMapping;
	}
	
	private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) {
		if (certificate == null || sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable)) {
			logger.Error("Could not establish a secure connection, server did not provide a certificate.");
		}
		else if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch)) {
			logger.Error("Could not establish a secure connection, server certificate has the wrong name: {Name}", certificate.Subject);
		}
		else if (!parameters.CertificateThumbprint.Check(certificate)) {
			logger.Error("Could not establish a secure connection, server certificate does not match.");
		}
		else if (TlsSupport.CheckAlgorithm((X509Certificate2) certificate) is {} error) {
			logger.Error("Could not establish a secure connection, server certificate rejected because it uses {ActualAlgorithmName} instead of {ExpectedAlgorithmName}.", error.ActualAlgorithmName, error.ExpectedAlgorithmName);
		}
		else if ((sslPolicyErrors & ~SslPolicyErrors.RemoteCertificateChainErrors) != SslPolicyErrors.None) {
			logger.Error("Could not establish a secure connection, server certificate validation failed.");
		}
		else {
			return true;
		}
		
		loggedCertificateValidationError = true;
		return false;
	}
	
	private static async Task DisconnectSocket(Socket socket, RpcStream? stream) {
		if (stream != null) {
			await stream.DisposeAsync();
		}
		
		using CancellationTokenSource timeoutTokenSource = new CancellationTokenSource(DisconnectTimeout);
		await socket.DisconnectAsync(reuseSocket: false, timeoutTokenSource.Token);
	}
	
	private readonly record struct ConnectionResult(bool IsNewSession, TimeSpan PingInterval, MessageTypeMappings<TClientToServerMessage, TServerToClientMessage> MessageTypeMappings);
	
	internal sealed record Connection(Socket Socket, RpcStream Stream, bool IsNewSession, TimeSpan PingInterval, MessageTypeMappings<TClientToServerMessage, TServerToClientMessage> MessageTypeMappings) : IAsyncDisposable {
		public async Task Disconnect() {
			await DisconnectSocket(Socket, Stream);
		}
		
		public async ValueTask Shutdown() {
			await Stream.DisposeAsync();
			Socket.Shutdown(SocketShutdown.Both);
		}
		
		public async ValueTask DisposeAsync() {
			await Stream.DisposeAsync();
			Socket.Close();
		}
	}
}
