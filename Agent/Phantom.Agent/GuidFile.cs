using System.Text;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent;

static class GuidFile {
	private static ILogger Logger { get; } = PhantomLogger.Create(nameof(GuidFile));
	
	private const string GuidFileName = "agent.guid";
	
	public static async Task<Guid?> CreateOrLoad(string folderPath) {
		string filePath = Path.Combine(folderPath, GuidFileName);
		
		if (File.Exists(filePath)) {
			try {
				var guid = await LoadGuidFromFile(filePath);
				Logger.Information("Loaded existing agent GUID file.");
				return guid;
			} catch (Exception e) {
				Logger.Fatal("Error reading agent GUID file: {Message}", e.Message);
				return null;
			}
		}
		
		Logger.Information("Creating agent GUID file: {FilePath}", filePath);
		
		try {
			var guid = Guid.NewGuid();
			await File.WriteAllTextAsync(filePath, guid.ToString(), Encoding.ASCII);
			return guid;
		} catch (Exception e) {
			Logger.Fatal("Error creating agent GUID file: {Message}", e.Message);
			return null;
		}
	}
	
	private static async Task<Guid> LoadGuidFromFile(string filePath) {
		Files.RequireMaximumFileSize(filePath, maximumBytes: 128);
		string contents = await File.ReadAllTextAsync(filePath, Encoding.ASCII);
		return Guid.Parse(contents.Trim());
	}
}
