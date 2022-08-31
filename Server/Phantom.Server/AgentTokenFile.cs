using Phantom.Common.Data.Agent;
using Phantom.Common.Logging;
using Serilog;

namespace Phantom.Server;

static class AgentTokenFile {
	private static ILogger Logger { get; } = PhantomLogger.Create(typeof(AgentTokenFile));

	private const string TokenFileName = "agent.token";

	public static async Task<AgentAuthToken?> CreateOrLoad(string folderPath) {
		string filePath = Path.Combine(folderPath, TokenFileName);

		if (File.Exists(filePath)) {
			try {
				var token = await AgentAuthToken.ReadFromFile(filePath);
				Logger.Information("Loaded existing agent token file.");
				return token;
			} catch (Exception e) {
				Logger.Fatal("Error reading agent token file: {Message}", e.Message);
				return null;
			}
		}

		Logger.Information("Creating agent token file: {FilePath}", filePath);

		try {
			var agentToken = AgentAuthToken.Generate();
			await agentToken.WriteToFile(filePath);
			return agentToken;
		} catch (Exception e) {
			Logger.Fatal("Error creating agent token file: {Message}", e.Message);
			return null;
		}
	}
}
