using NetMQ;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Server.Application;

static class Certificates {
	private static ILogger Logger { get; } = PhantomLogger.Create(typeof(Certificates));

	private const string SecretKeyFileName = "secret.key";
	private const string PublicKeyFileName = "agent.key";

	public static async Task<NetMQCertificate?> CreateOrLoad(string folderPath) {
		if (!Directory.Exists(folderPath)) {
			try {
				Directories.Create(folderPath, Chmod.URW_GR);
			} catch (Exception e) {
				Logger.Fatal(e, "Error creating certificate folder.");
				return null;
			}
		}

		string secretKeyFilePath = Path.Combine(folderPath, SecretKeyFileName);
		string publicKeyFilePath = Path.Combine(folderPath, PublicKeyFileName);

		var secretKeyFileExists = File.Exists(secretKeyFilePath);
		var publicKeyFileExists = File.Exists(publicKeyFilePath);

		if (secretKeyFileExists && publicKeyFileExists) {
			try {
				return await LoadCertificateFiles(secretKeyFilePath, publicKeyFilePath);
			} catch (Exception e) {
				Logger.Fatal(e, "Error reading certificate files.");
				return null;
			}
		}

		if (secretKeyFileExists || publicKeyFileExists) {
			string existingKeyFilePath = secretKeyFileExists ? secretKeyFilePath : publicKeyFilePath;
			string missingKeyFileName = secretKeyFileExists ? PublicKeyFileName : SecretKeyFileName;
			Logger.Fatal("The certificate folder contains {ExistingKeyFilePath} but no {MissingKeyFileName} file. Please delete it to regenerate both certificate files.", existingKeyFilePath, missingKeyFileName);
			return null;
		}

		Logger.Information("Generating certificate files in: {FolderPath}", folderPath);
		
		try {
			return await GenerateCertificateFiles(secretKeyFilePath, publicKeyFilePath);
		} catch (Exception e) {
			Logger.Fatal(e, "Error creating certificate files.");
			return null;
		}
	}

	private static async Task<NetMQCertificate?> LoadCertificateFiles(string secretKeyFilePath, string publicKeyFilePath) {
		byte[] secretKey = await File.ReadAllBytesAsync(secretKeyFilePath);
		byte[] publicKey = await File.ReadAllBytesAsync(publicKeyFilePath);

		var certificate = new NetMQCertificate(secretKey, publicKey);
		Logger.Information("Loaded existing certificates. Remember that agents will need {PublicKeyFilePath} to connect.", publicKeyFilePath);
		return certificate;
	}

	private static async Task<NetMQCertificate?> GenerateCertificateFiles(string secretKeyFilePath, string publicKeyFilePath) {
		var certificate = new NetMQCertificate();

		await Files.WriteBytesAsync(secretKeyFilePath, certificate.SecretKey, FileMode.Create, Chmod.URW_GR);
		await Files.WriteBytesAsync(publicKeyFilePath, certificate.PublicKey, FileMode.Create, Chmod.URW_GR);

		Logger.Information("Certificates created. Agents will need {PublicKeyFilePath} to connect.", publicKeyFilePath);
		return certificate;
	}
}
