using NetMQ;
using Phantom.Common.Logging;
using Phantom.Utils.IO;
using Serilog;

namespace Phantom.Server;

static class CertificateFiles {
	private static ILogger Logger { get; } = PhantomLogger.Create(typeof(CertificateFiles));

	private const string SecretKeyFileName = "secret.key";
	private const string PublicKeyFileName = "agent.key";

	public static async Task<NetMQCertificate?> CreateOrLoad(string folderPath) {
		string secretKeyFilePath = Path.Combine(folderPath, SecretKeyFileName);
		string publicKeyFilePath = Path.Combine(folderPath, PublicKeyFileName);

		bool secretKeyFileExists = File.Exists(secretKeyFilePath);
		bool publicKeyFileExists = File.Exists(publicKeyFilePath);

		if (secretKeyFileExists && publicKeyFileExists) {
			try {
				return await LoadCertificatesFromFiles(secretKeyFilePath, publicKeyFilePath);
			} catch (Exception e) {
				Logger.Fatal("Error reading certificate files.");
				Logger.Fatal(e.Message);
				return null;
			}
		}

		if (secretKeyFileExists || publicKeyFileExists) {
			string existingKeyFilePath = secretKeyFileExists ? secretKeyFilePath : publicKeyFilePath;
			string missingKeyFileName = secretKeyFileExists ? PublicKeyFileName : SecretKeyFileName;
			Logger.Fatal("The certificate file {ExistingKeyFilePath} exists but {MissingKeyFileName} does not. Please delete it to regenerate both certificate files.", existingKeyFilePath, missingKeyFileName);
			return null;
		}

		Logger.Information("Creating certificate files in: {FolderPath}", folderPath);
		
		try {
			return await GenerateCertificateFiles(secretKeyFilePath, publicKeyFilePath);
		} catch (Exception e) {
			Logger.Fatal("Error creating certificate files.");
			Logger.Fatal(e.Message);
			return null;
		}
	}

	private static async Task<NetMQCertificate?> LoadCertificatesFromFiles(string secretKeyFilePath, string publicKeyFilePath) {
		byte[] secretKey = await ReadCertificateFile(secretKeyFilePath);
		byte[] publicKey = await ReadCertificateFile(publicKeyFilePath);

		var certificate = new NetMQCertificate(secretKey, publicKey);
		Logger.Information("Loaded existing certificates. Remember that agents will need {PublicKeyFilePath} to connect.", publicKeyFilePath);
		return certificate;
	}
	
	private static async Task<byte[]> ReadCertificateFile(string filePath) {
		Files.RequireMaximumFileSize(filePath, 1024);
		return await File.ReadAllBytesAsync(filePath);
	}

	private static async Task<NetMQCertificate?> GenerateCertificateFiles(string secretKeyFilePath, string publicKeyFilePath) {
		var certificate = new NetMQCertificate();

		await Files.WriteBytesAsync(secretKeyFilePath, certificate.SecretKey, FileMode.Create, Chmod.URW_GR);
		await Files.WriteBytesAsync(publicKeyFilePath, certificate.PublicKey, FileMode.Create, Chmod.URW_GR);

		Logger.Information("Certificates created. Agents will need {PublicKeyFilePath} to connect.", publicKeyFilePath);
		return certificate;
	}
}
