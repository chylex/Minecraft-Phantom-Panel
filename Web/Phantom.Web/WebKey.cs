using Phantom.Common.Data;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using ILogger = Serilog.ILogger;

namespace Phantom.Web;

static class WebKey {
	private static ILogger Logger { get; } = PhantomLogger.Create(nameof(WebKey));
	
	public static Task<ConnectionKey?> Load(string? webKeyToken, string? webKeyFilePath) {
		if (webKeyFilePath != null) {
			return LoadFromFile(webKeyFilePath);
		}
		else if (webKeyToken != null) {
			return Task.FromResult(LoadFromToken(webKeyToken));
		}
		else {
			throw new InvalidOperationException();
		}
	}
	
	private static async Task<ConnectionKey?> LoadFromFile(string webKeyFilePath) {
		if (!File.Exists(webKeyFilePath)) {
			Logger.Fatal("Missing web key file: {WebKeyFilePath}", webKeyFilePath);
			return null;
		}
		
		try {
			Files.RequireMaximumFileSize(webKeyFilePath, maximumBytes: 64);
			return LoadFromBytes(await File.ReadAllBytesAsync(webKeyFilePath));
		} catch (IOException e) {
			Logger.Fatal("Error loading web key from file: {WebKeyFilePath}", webKeyFilePath);
			Logger.Fatal("{Message}", e.Message);
			return null;
		} catch (Exception) {
			Logger.Fatal("File does not contain a valid web key: {WebKeyFilePath}", webKeyFilePath);
			return null;
		}
	}
	
	private static ConnectionKey? LoadFromToken(string webKey) {
		try {
			return LoadFromBytes(TokenGenerator.DecodeBytes(webKey));
		} catch (Exception) {
			Logger.Fatal("Invalid web key: {WebKey}", webKey);
			return null;
		}
	}
	
	private static ConnectionKey? LoadFromBytes(byte[] webKey) {
		var connectionKey = ConnectionKey.FromBytes(webKey);
		Logger.Information("Loaded web key.");
		return connectionKey;
	}
}
