using System.Text;
using Phantom.Common.Data;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent;

static class AgentKey {
	private static ILogger Logger { get; } = PhantomLogger.Create(nameof(AgentKey));
	
	public static Task<ConnectionKey?> Load(string? agentKeyToken, string? agentKeyFilePath) {
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
	
	private static async Task<ConnectionKey?> LoadFromFile(string agentKeyFilePath) {
		if (!File.Exists(agentKeyFilePath)) {
			Logger.Fatal("Missing agent key file: {AgentKeyFilePath}", agentKeyFilePath);
			return null;
		}
		
		try {
			Files.RequireMaximumFileSize(agentKeyFilePath, maximumBytes: 128);
			string[] lines = await File.ReadAllLinesAsync(agentKeyFilePath, Encoding.UTF8);
			return LoadFromToken(lines[0]);
		} catch (IOException e) {
			Logger.Fatal("Error loading agent key from file: {AgentKeyFilePath}", agentKeyFilePath);
			Logger.Fatal("{Message}", e.Message);
			return null;
		} catch (Exception) {
			Logger.Fatal("File does not contain a valid agent key: {AgentKeyFilePath}", agentKeyFilePath);
			return null;
		}
	}
	
	private static ConnectionKey? LoadFromToken(string agentKey) {
		try {
			return LoadFromBytes(TokenGenerator.DecodeBytes(agentKey));
		} catch (Exception) {
			Logger.Fatal("Invalid agent key: {AgentKey}", agentKey);
			return null;
		}
	}
	
	private static ConnectionKey? LoadFromBytes(byte[] agentKey) {
		var connectionKey = ConnectionKey.FromBytes(agentKey);
		Logger.Information("Loaded agent key.");
		return connectionKey;
	}
}
