using System.Globalization;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Phantom.Utils.Logging;

public static class PhantomLogger {
	public static Logger Base { get; } = CreateBaseLogger();

	private static LogEventLevel GetDefaultLevel() {
		#if DEBUG
		return LogEventLevel.Debug;
		#else
		return LogEventLevel.Information;
		#endif
	}

	private static Logger CreateBaseLogger() =>
		new LoggerConfiguration()
			.MinimumLevel.Is(GetDefaultLevel())
			.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
			.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
			.Enrich.FromLogContext()
			.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u}] {Message:lj}{NewLine}{Exception}", formatProvider: CultureInfo.InvariantCulture, theme: AnsiConsoleTheme.Literate)
			.CreateLogger();

	public static ILogger Create<T>() {
		return Base.ForContext<T>();
	}

	public static ILogger Create(string name) {
		return Base.ForContext(Constants.SourceContextPropertyName, name);
	}
}
