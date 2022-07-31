using NetMQ;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent;

static class Certificates {
	private static ILogger Logger { get; } = PhantomLogger.Create(typeof(Certificates));

	public static async Task<NetMQCertificate?> LoadPublicKey(string publicKeyFilePath) {
		if (!File.Exists(publicKeyFilePath)) {
			Logger.Fatal("Cannot load server certificate, missing key file: {PublicKeyFilePath}", publicKeyFilePath);
			return null;
		}

		try {
			byte[] publicKey = await File.ReadAllBytesAsync(publicKeyFilePath);
			return NetMQCertificate.FromPublicKey(publicKey);
		} catch (Exception e) {
			Logger.Fatal(e, "Error loading server certificate from key file: {PublicKeyFilePath}", publicKeyFilePath);
			return null;
		}
	}
}
