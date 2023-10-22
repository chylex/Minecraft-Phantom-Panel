using Serilog;

namespace Phantom.Common.Logging;

public static class LoggerExtensions {
	private static readonly string HeadingPadding = new (' ', 23);
	private static readonly string HeadingLine = new ('-', Math.Min(50, Console.BufferWidth));
	
	private static readonly string Heading1 = HeadingLine + '\n' + HeadingPadding;
	private static readonly string Heading2 = '\n' + HeadingPadding + HeadingLine;

	public static void InformationHeading(this ILogger logger, string title) {
		logger.Information("{Heading1}{Title}{Heading2}", Heading1, title, Heading2);
	}
}
