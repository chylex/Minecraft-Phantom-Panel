using NetMQ;
using Phantom.Common.Data.Agent;
using Phantom.Common.Logging;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;
using Serilog;

namespace Phantom.Agent;

static class AgentKey {
	private static ILogger Logger { get; } = PhantomLogger.Create(typeof(AgentKey));

	public static Task<(NetMQCertificate, AgentAuthToken)?> Load(string? agentKeyToken, string? agentKeyFilePath) {
		if (agentKeyFilePath != null) {
			return LoadFromFile(agentKeyFilePath);
		}
		else if (agentKeyToken != null) {
			return Task.FromResult(LoadFromToken(agentKeyToken));
		}
		else {
			throw new InvalidOperationException();
		}
	}

	private static async Task<(NetMQCertificate, AgentAuthToken)?> LoadFromFile(string agentKeyFilePath) {
		if (!File.Exists(agentKeyFilePath)) {
			Logger.Fatal("Missing agent key file: {AgentKeyFilePath}", agentKeyFilePath);
			return null;
		}

		try {
			Files.RequireMaximumFileSize(agentKeyFilePath, 64);
			return LoadFromBytes(await File.ReadAllBytesAsync(agentKeyFilePath));
		} catch (IOException e) {
			Logger.Fatal("Error loading agent key from file: {AgentKeyFilePath}", agentKeyFilePath);
			Logger.Fatal(e.Message);
			return null;
		} catch (Exception) {
			Logger.Fatal("File does not contain a valid agent key: {AgentKeyFilePath}", agentKeyFilePath);
			return null;
		}
	}

	private static (NetMQCertificate, AgentAuthToken)? LoadFromToken(string agentKey) {
		try {
			return LoadFromBytes(TokenGenerator.DecodeBytes(agentKey));
		} catch (Exception) {
			Logger.Fatal("Invalid agent key: {AgentKey}", agentKey);
			return null;
		}
	}

	private static (NetMQCertificate, AgentAuthToken)? LoadFromBytes(byte[] agentKey) {
		var (publicKey, agentToken) = AgentKeyData.FromBytes(agentKey);
		var serverCertificate = NetMQCertificate.FromPublicKey(publicKey);
		
		Logger.Information("Loaded agent key.");
		return (serverCertificate, agentToken);
	}
}
