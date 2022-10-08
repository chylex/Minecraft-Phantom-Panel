using ILogger = Serilog.ILogger;

namespace Phantom.Server.Web;

public sealed record Configuration(ILogger Logger, string Host, ushort Port, CancellationToken CancellationToken) {
	public string HttpUrl => "http://" + Host + ":" + Port;
}
