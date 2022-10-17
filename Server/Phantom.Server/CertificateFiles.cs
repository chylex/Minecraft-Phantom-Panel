using NetMQ;
using Phantom.Common.Data.Agent;
using Phantom.Common.Logging;
using Phantom.Utils.IO;
using Serilog;

namespace Phantom.Server;

static class CertificateFiles {
	private static ILogger Logger { get; } = PhantomLogger.Create(typeof(CertificateFiles));

	private const string SecretKeyFileName = "secret.key";
	private const string AgentKeyFileName = "agent.key";

	public static async Task<(NetMQCertificate, AgentAuthToken)?> CreateOrLoad(string folderPath) {
		string secretKeyFilePath = Path.Combine(folderPath, SecretKeyFileName);
		string agentKeyFilePath = Path.Combine(folderPath, AgentKeyFileName);

		bool secretKeyFileExists = File.Exists(secretKeyFilePath);
		bool agentKeyFileExists = File.Exists(agentKeyFilePath);

		if (secretKeyFileExists && agentKeyFileExists) {
			try {
				return await LoadCertificatesFromFiles(secretKeyFilePath, agentKeyFilePath);
			} catch (Exception e) {
				Logger.Fatal("Error reading certificate files.");
				Logger.Fatal(e.Message);
				return null;
			}
		}

		if (secretKeyFileExists || agentKeyFileExists) {
			string existingKeyFilePath = secretKeyFileExists ? secretKeyFilePath : agentKeyFilePath;
			string missingKeyFileName = secretKeyFileExists ? AgentKeyFileName : SecretKeyFileName;
			Logger.Fatal("The certificate file {ExistingKeyFilePath} exists but {MissingKeyFileName} does not. Please delete it to regenerate both certificate files.", existingKeyFilePath, missingKeyFileName);
			return null;
		}

		Logger.Information("Creating certificate files in: {FolderPath}", folderPath);
		
		try {
			return await GenerateCertificateFiles(secretKeyFilePath, agentKeyFilePath);
		} catch (Exception e) {
			Logger.Fatal("Error creating certificate files.");
			Logger.Fatal(e.Message);
			return null;
		}
	}

	private static async Task<(NetMQCertificate, AgentAuthToken)?> LoadCertificatesFromFiles(string secretKeyFilePath, string agentKeyFilePath) {
		byte[] secretKey = await ReadCertificateFile(secretKeyFilePath);
		byte[] agentKey = await ReadCertificateFile(agentKeyFilePath);

		var (publicKey, agentToken) = AgentKeyData.FromBytes(agentKey);
		var certificate = new NetMQCertificate(secretKey, publicKey);
		
		Logger.Information("Loaded existing certificate files. Agents will need {AgentKeyFilePath} to connect.", agentKeyFilePath);
		return (certificate, agentToken);
	}

	private static Task<byte[]> ReadCertificateFile(string filePath) {
		Files.RequireMaximumFileSize(filePath, 64);
		return File.ReadAllBytesAsync(filePath);
	}

	private static async Task<(NetMQCertificate, AgentAuthToken)> GenerateCertificateFiles(string secretKeyFilePath, string agentKeyFilePath) {
		var certificate = new NetMQCertificate();
		var agentToken = AgentAuthToken.Generate();

		await Files.WriteBytesAsync(secretKeyFilePath, certificate.SecretKey, FileMode.Create, Chmod.URW_GR);
		await Files.WriteBytesAsync(agentKeyFilePath, AgentKeyData.ToBytes(certificate.PublicKey, agentToken), FileMode.Create, Chmod.URW_GR);

		Logger.Information("Certificates created. Agents will need {AgentKeyFilePath} to connect.", agentKeyFilePath);
		return (certificate, agentToken);
	}
}
