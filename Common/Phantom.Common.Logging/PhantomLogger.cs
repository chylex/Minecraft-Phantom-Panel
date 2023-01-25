using System.Globalization;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Phantom.Common.Logging;

public static class PhantomLogger {
	public static Logger Root { get; } = CreateBaseLogger("[{Timestamp:HH:mm:ss} {Level:u}] {Message:lj}{NewLine}{Exception}");
	private static Logger Base { get; } = CreateBaseLogger("[{Timestamp:HH:mm:ss} {Level:u}] [{Category}] {Message:lj}{NewLine}{Exception}");

	private static LogEventLevel GetDefaultLevel() {
		#if DEBUG
		return LogEventLevel.Verbose;
		#else
		return LogEventLevel.Information;
		#endif
	}

	private static Logger CreateBaseLogger(string template) =>
		new LoggerConfiguration()
			.MinimumLevel.Is(GetDefaultLevel())
			.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
			.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
			.MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
			.Filter.ByExcluding(static e => e.Exception is OperationCanceledException)
			.Enrich.FromLogContext()
			.WriteTo.Console(outputTemplate: template, formatProvider: CultureInfo.InvariantCulture, theme: AnsiConsoleTheme.Literate)
			.CreateLogger();

	public static ILogger Create(string name) {
		return Base.ForContext("Category", name);
	}

	public static ILogger Create(string name1, string name2) {
		return Create(name1 + ":" + name2);
	}

	public static ILogger Create<T>() {
		return Create(typeof(T).Name);
	}

	public static ILogger Create<T>(string name) {
		return Create(typeof(T).Name, name);
	}

	public static ILogger Create<T1, T2>() {
		return Create(typeof(T1).Name, typeof(T2).Name);
	}

	public static void Dispose() {
		Root.Dispose();
		Base.Dispose();
	}
}
