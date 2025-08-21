using NetMQ;
using Phantom.Common.Data;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Controller;

abstract class ConnectionKeyFiles {
	private const string CommonKeyFileExtension = ".key";
	private const string SecretKeyFileExtension = ".secret";
	
	private readonly ILogger logger;
	private readonly string commonKeyFileName;
	private readonly string secretKeyFileName;
	
	private ConnectionKeyFiles(ILogger logger, string name) {
		this.logger = logger;
		this.commonKeyFileName = name + CommonKeyFileExtension;
		this.secretKeyFileName = name + SecretKeyFileExtension;
	}
	
	public async Task<ConnectionKeyData?> CreateOrLoad(string folderPath) {
		string commonKeyFilePath = Path.Combine(folderPath, commonKeyFileName);
		string secretKeyFilePath = Path.Combine(folderPath, secretKeyFileName);
		
		bool commonKeyFileExists = File.Exists(commonKeyFilePath);
		bool secretKeyFileExists = File.Exists(secretKeyFilePath);
		
		if (commonKeyFileExists && secretKeyFileExists) {
			try {
				return await ReadKeyFiles(commonKeyFilePath, secretKeyFilePath);
			} catch (IOException e) {
				logger.Fatal("Error reading connection key files.");
				logger.Fatal(e.Message);
				return null;
			} catch (Exception) {
				logger.Fatal("Connection key files contain invalid data.");
				return null;
			}
		}
		
		if (commonKeyFileExists || secretKeyFileExists) {
			string existingKeyFilePath = commonKeyFileExists ? commonKeyFilePath : secretKeyFilePath;
			string missingKeyFileName = commonKeyFileExists ? secretKeyFileName : commonKeyFileName;
			logger.Fatal("The connection key file {ExistingKeyFilePath} exists but {MissingKeyFileName} does not. Please delete it to regenerate both files.", existingKeyFilePath, missingKeyFileName);
			return null;
		}
		
		logger.Information("Creating connection key files in: {FolderPath}", folderPath);
		
		try {
			return await GenerateKeyFiles(commonKeyFilePath, secretKeyFilePath);
		} catch (Exception e) {
			logger.Fatal("Error creating connection key files.");
			logger.Fatal(e.Message);
			return null;
		}
	}
	
	private async Task<ConnectionKeyData?> ReadKeyFiles(string commonKeyFilePath, string secretKeyFilePath) {
		byte[] commonKeyBytes = await ReadKeyFile(commonKeyFilePath);
		byte[] secretKeyBytes = await ReadKeyFile(secretKeyFilePath);
		
		var (publicKey, authToken) = ConnectionCommonKey.FromBytes(commonKeyBytes);
		var certificate = new NetMQCertificate(secretKeyBytes, publicKey);
		
		logger.Information("Loaded connection key files.");
		LogCommonKey(commonKeyFilePath, TokenGenerator.EncodeBytes(commonKeyBytes));
		
		return new ConnectionKeyData(certificate, authToken);
	}
	
	private static Task<byte[]> ReadKeyFile(string filePath) {
		Files.RequireMaximumFileSize(filePath, 64);
		return File.ReadAllBytesAsync(filePath);
	}
	
	private async Task<ConnectionKeyData> GenerateKeyFiles(string commonKeyFilePath, string secretKeyFilePath) {
		var certificate = new NetMQCertificate();
		var authToken = AuthToken.Generate();
		var commonKey = new ConnectionCommonKey(certificate.PublicKey, authToken).ToBytes();
		
		await Files.WriteBytesAsync(secretKeyFilePath, certificate.SecretKey, FileMode.Create, Chmod.URW_GR);
		await Files.WriteBytesAsync(commonKeyFilePath, commonKey, FileMode.Create, Chmod.URW_GR);
		
		logger.Information("Created new connection key files.");
		LogCommonKey(commonKeyFilePath, TokenGenerator.EncodeBytes(commonKey));
		
		return new ConnectionKeyData(certificate, authToken);
	}
	
	protected abstract void LogCommonKey(string commonKeyFilePath, string commonKeyEncoded);
	
	internal sealed class Agent : ConnectionKeyFiles {
		public Agent() : base(PhantomLogger.Create<ConnectionKeyFiles, Agent>(), "agent") {}
		
		protected override void LogCommonKey(string commonKeyFilePath, string commonKeyEncoded) {
			logger.Information("Agent key file: {AgentKeyFilePath}", commonKeyFilePath);
			logger.Information("Agent key: {AgentKey}", commonKeyEncoded);
		}
	}
	
	internal sealed class Web : ConnectionKeyFiles {
		public Web() : base(PhantomLogger.Create<ConnectionKeyFiles, Web>(), "web") {}
		
		protected override void LogCommonKey(string commonKeyFilePath, string commonKeyEncoded) {
			logger.Information("Web key file: {WebKeyFilePath}", commonKeyFilePath);
			logger.Information("Web key: {WebKey}", commonKeyEncoded);
		}
	}
}
