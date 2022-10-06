using NetMQ;
using Phantom.Common.Logging;
using Phantom.Utils.IO;
using Serilog;

namespace Phantom.Agent;

static class CertificateFile {
	private static ILogger Logger { get; } = PhantomLogger.Create(typeof(CertificateFile));

	public static async Task<NetMQCertificate?> LoadPublicKey(string publicKeyFilePath) {
		if (!File.Exists(publicKeyFilePath)) {
			Logger.Fatal("Cannot load server certificate, missing key file: {PublicKeyFilePath}", publicKeyFilePath);
			return null;
		}

		try {
			var publicKey = await LoadPublicKeyFromFile(publicKeyFilePath);
			Logger.Information("Loaded server certificate.");
			return publicKey;
		} catch (Exception e) {
			Logger.Fatal(e, "Error loading server certificate from key file: {PublicKeyFilePath}", publicKeyFilePath);
			return null;
		}
	}

	private static async Task<NetMQCertificate> LoadPublicKeyFromFile(string filePath) {
		Files.RequireMaximumFileSize(filePath, 1024);
		byte[] publicKey = await File.ReadAllBytesAsync(filePath);
		return NetMQCertificate.FromPublicKey(publicKey);
	}
}
