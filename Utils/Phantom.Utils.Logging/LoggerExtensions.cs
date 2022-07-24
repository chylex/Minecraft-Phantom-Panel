using Serilog;

namespace Phantom.Utils.Logging; 

public static class LoggerExtensions {
	private static readonly string HeadingLine = '\n' + new string ('-', Math.Min(50, Console.BufferWidth));

	public static void InformationHeading(this ILogger logger, string title) {
		logger.Information(HeadingLine + '\n' + title + HeadingLine);
	}
}
