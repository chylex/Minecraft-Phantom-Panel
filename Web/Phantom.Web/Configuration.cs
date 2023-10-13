using ILogger = Serilog.ILogger;

namespace Phantom.Web;

sealed record Configuration(ILogger Logger, string Host, ushort Port, string BasePath, string DataProtectionKeyFolderPath, CancellationToken CancellationToken) {
	public string HttpUrl => "http://" + Host + ":" + Port;
}
