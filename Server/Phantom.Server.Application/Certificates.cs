using NetMQ;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Server.Application;

static class Certificates {
	private static ILogger Logger { get; } = PhantomLogger.Create(typeof(Certificates));

	private const string SecretKeyFileName = "secret.key";
	private const string PublicKeyFileName = "agent.key";

	public static async Task<bool> CreateIfNeeded(string folderPath) {
		if (!Directory.Exists(folderPath)) {
			Logger.Information("Creating folder for certificates: {FolderPath}", folderPath);
			
			try {
				Directories.Create(folderPath, Chmod.URW_GR);
			} catch (Exception e) {
				Logger.Fatal(e, "Error creating certificate folder.");
				return false;
			}
		}

		string secretKeyFilePath = Path.Combine(folderPath, SecretKeyFileName);
		string publicKeyFilePath = Path.Combine(folderPath, PublicKeyFileName);

		var secretKeyFileExists = File.Exists(secretKeyFilePath);
		var publicKeyFileExists = File.Exists(publicKeyFilePath);

		if (secretKeyFileExists && publicKeyFileExists) {
			return true;
		}
		else if (secretKeyFileExists || publicKeyFileExists) {
			string existingKeyFilePath = secretKeyFileExists ? secretKeyFilePath : publicKeyFilePath;
			string missingKeyFileName = secretKeyFileExists ? PublicKeyFileName : SecretKeyFileName;
			Logger.Fatal("The certificate folder contains {ExistingKeyFilePath} but no {MissingKeyFileName} file. Please delete it to regenerate both certificate files.", existingKeyFilePath, missingKeyFileName);
			return false;
		}

		Logger.Information("Generating certificate files in: {FolderPath}", folderPath);
		
		var certificate = new NetMQCertificate();

		try {
			await Files.WriteBytesAsync(secretKeyFilePath, certificate.SecretKey, FileMode.Create, Chmod.URW_GR);
			await Files.WriteBytesAsync(publicKeyFilePath, certificate.PublicKey, FileMode.Create, Chmod.URW_GR);
		} catch (Exception e) {
			Logger.Fatal(e, "Error creating certificate files.");
			return false;
		}

		Logger.Information("Certificates created! Agents will need {PublicKeyFilePath} to connect.", publicKeyFilePath);
		return true;
	}
}
