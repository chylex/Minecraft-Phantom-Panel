using System.Text;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Server;

static class AgentTokenFile {
	private static ILogger Logger { get; } = PhantomLogger.Create(typeof(AgentTokenFile));

	private const string TokenFileName = "agent.token";

	private const int MinimumTokenLength = 30;
	private const int MaximumTokenLength = 100;

	public static async Task<string?> CreateOrLoad(string folderPath) {
		string filePath = Path.Combine(folderPath, TokenFileName);

		if (File.Exists(filePath)) {
			try {
				return await LoadTokenFromFile(filePath);
			} catch (Exception e) {
				Logger.Fatal("Error reading agent token file.");
				Logger.Fatal(e.Message);
				return null;
			}
		}

		Logger.Information("Creating agent token file: {FilePath}", filePath);
		
		string agentToken = TokenGenerator.Create(MinimumTokenLength);

		try {
			await Files.WriteBytesAsync(filePath, TokenGenerator.GetBytesOrThrow(agentToken), FileMode.Create, Chmod.URW_GR);
			return agentToken;
		} catch (Exception e) {
			Logger.Fatal("Error creating agent token file.");
			Logger.Fatal(e.Message);
			return null;
		}
	}

	private static async Task<string?> LoadTokenFromFile(string filePath) {
		Files.RequireMaximumFileSize(filePath, MaximumTokenLength + 1);
		string agentToken = (await File.ReadAllTextAsync(filePath, Encoding.ASCII)).Trim();

		int tokenLength = agentToken.Length;
		if (tokenLength is < MinimumTokenLength or > MaximumTokenLength) {
			throw new Exception("Invalid token length: " + tokenLength + ". Token length must be between " + MinimumTokenLength + " and " + MaximumTokenLength + ".");
		}

		TokenGenerator.GetBytesOrThrow(agentToken);
		Logger.Information("Loaded existing agent token file.");
		return agentToken;
	}
}
