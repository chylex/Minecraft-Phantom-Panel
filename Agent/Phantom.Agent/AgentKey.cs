using NetMQ;
using Phantom.Common.Data.Agent;
using Phantom.Common.Logging;
using Phantom.Utils.IO;
using Serilog;

namespace Phantom.Agent;

static class AgentKey {
	private static ILogger Logger { get; } = PhantomLogger.Create(typeof(AgentKey));

	public static async Task<(NetMQCertificate, AgentAuthToken)?> LoadFromFile(string agentKeyFilePath) {
		if (!File.Exists(agentKeyFilePath)) {
			Logger.Fatal("Cannot load agent key, missing key file: {AgentKeyFilePath}", agentKeyFilePath);
			return null;
		}

		try {
			Files.RequireMaximumFileSize(agentKeyFilePath, 64);
			var (publicKey, agentToken) = AgentKeyData.FromBytes(await File.ReadAllBytesAsync(agentKeyFilePath));
			var serverCertificate = NetMQCertificate.FromPublicKey(publicKey);
			Logger.Information("Loaded agent key.");
			return (serverCertificate, agentToken);
		} catch (Exception e) {
			Logger.Fatal(e, "Error loading agent key from key file: {AgentKeyFilePath}", agentKeyFilePath);
			return null;
		}
	}
}
