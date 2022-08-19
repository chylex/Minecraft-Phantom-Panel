using NetMQ;
using Phantom.Common.Logging;
using Phantom.Utils.IO;
using Serilog;

namespace Phantom.Agent;

static class CertificateFiles {
	private static ILogger Logger { get; } = PhantomLogger.Create(typeof(CertificateFiles));

	public static async Task<NetMQCertificate?> LoadPublicKey(string publicKeyFilePath) {
		if (!File.Exists(publicKeyFilePath)) {
			Logger.Fatal("Cannot load server certificate, missing key file: {PublicKeyFilePath}", publicKeyFilePath);
			return null;
		}

		try {
			Files.RequireMaximumFileSize(publicKeyFilePath, 1024);
			byte[] publicKey = await File.ReadAllBytesAsync(publicKeyFilePath);
			return NetMQCertificate.FromPublicKey(publicKey);
		} catch (Exception e) {
			Logger.Fatal(e, "Error loading server certificate from key file: {PublicKeyFilePath}", publicKeyFilePath);
			return null;
		}
	}
}
