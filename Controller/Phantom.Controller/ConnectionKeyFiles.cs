using Phantom.Common.Data;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Phantom.Utils.Monads;
using Phantom.Utils.Rpc;
using Phantom.Utils.Rpc.Runtime.Tls;
using Serilog;

namespace Phantom.Controller;

abstract class ConnectionKeyFiles {
	private readonly ILogger logger;
	private readonly string certificateFileName;
	private readonly string authTokenFileName;
	
	private ConnectionKeyFiles(ILogger logger, string name) {
		this.logger = logger;
		this.certificateFileName = name + ".pfx";
		this.authTokenFileName = name + ".auth";
	}
	
	public async Task<ConnectionKeyData?> CreateOrLoad(string folderPath) {
		string certificateFilePath = Path.Combine(folderPath, certificateFileName);
		string authTokenFilePath = Path.Combine(folderPath, authTokenFileName);
		
		bool certificateFileExists = File.Exists(certificateFilePath);
		bool authTokenFileExists = File.Exists(authTokenFilePath);
		
		if (certificateFileExists && authTokenFileExists) {
			try {
				return await ReadKeyFiles(certificateFilePath, authTokenFilePath);
			} catch (IOException e) {
				logger.Fatal(e, "Error reading connection key files.");
				return null;
			} catch (Exception) {
				logger.Fatal("Connection key files contain invalid data.");
				return null;
			}
		}
		
		if (certificateFileExists || authTokenFileExists) {
			string existingKeyFilePath = certificateFileExists ? certificateFilePath : authTokenFilePath;
			string missingKeyFileName = certificateFileExists ? authTokenFileName : certificateFileName;
			logger.Fatal("Connection key file {ExistingKeyFilePath} exists but {MissingKeyFileName} does not. Please delete it to regenerate both files.", existingKeyFilePath, missingKeyFileName);
			return null;
		}
		
		logger.Information("Creating connection key files in: {FolderPath}", folderPath);
		
		try {
			return await GenerateKeyFiles(certificateFilePath, authTokenFilePath);
		} catch (Exception e) {
			logger.Fatal(e, "Error creating connection key files.");
			return null;
		}
	}
	
	private async Task<ConnectionKeyData?> ReadKeyFiles(string certificateFilePath, string authTokenFilePath) {
		RpcServerCertificate certificate = null!;
		
		switch (RpcServerCertificate.Load(certificateFilePath)) {
			case Left<RpcServerCertificate, DisallowedAlgorithmError>(var rpcServerCertificate):
				certificate = rpcServerCertificate;
				break;
			
			case Right<RpcServerCertificate, DisallowedAlgorithmError>(var error):
				logger.Fatal("Certificate {CertificateFilePath} was expected to use {ExpectedAlgorithmName}, instead it uses {ActualAlgorithmName}.", certificateFilePath, error.ExpectedAlgorithmName, error.ActualAlgorithmName);
				return null;
		}
		
		var authToken = new AuthToken([..await ReadKeyFile(authTokenFilePath)]);
		logger.Information("Loaded connection key files.");
		
		var connectionKey = new ConnectionKey(certificate.Thumbprint, authToken);
		LogCommonKey(TokenGenerator.EncodeBytes(connectionKey.ToBytes()));
		
		return new ConnectionKeyData(certificate, authToken);
	}
	
	private static Task<byte[]> ReadKeyFile(string filePath) {
		Files.RequireMaximumFileSize(filePath, maximumBytes: 64);
		return File.ReadAllBytesAsync(filePath);
	}
	
	private async Task<ConnectionKeyData> GenerateKeyFiles(string certificateFilePath, string authTokenFilePath) {
		var certificateBytes = RpcServerCertificate.CreateAndExport("phantom-controller");
		var authToken = AuthToken.Generate();
		
		await Files.WriteBytesAsync(certificateFilePath, certificateBytes, FileMode.Create, Chmod.URW_GR);
		await Files.WriteBytesAsync(authTokenFilePath, authToken.Bytes.ToArray(), FileMode.Create, Chmod.URW_GR);
		logger.Information("Created new connection key files.");
		
		var certificate = RpcServerCertificate.Load(certificateFilePath).RequireLeft;
		var connectionKey = new ConnectionKey(certificate.Thumbprint, authToken);
		LogCommonKey(TokenGenerator.EncodeBytes(connectionKey.ToBytes()));
		
		return new ConnectionKeyData(certificate, authToken);
	}
	
	protected abstract void LogCommonKey(string commonKeyEncoded);
	
	internal sealed class Agent() : ConnectionKeyFiles(PhantomLogger.Create<ConnectionKeyFiles, Agent>(), "agent") {
		protected override void LogCommonKey(string commonKeyEncoded) {
			logger.Information("Agent key: {AgentKey}", commonKeyEncoded);
		}
	}
	
	internal sealed class Web() : ConnectionKeyFiles(PhantomLogger.Create<ConnectionKeyFiles, Web>(), "web") {
		protected override void LogCommonKey(string commonKeyEncoded) {
			logger.Information("Web key: {WebKey}", commonKeyEncoded);
		}
	}
}
