using System.Globalization;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Phantom.Common.Logging;

public static class PhantomLogger {
	public static Logger Root { get; } = CreateLogger("[{Timestamp:HH:mm:ss} {Level:u}] {Message:lj}{NewLine}{Exception}");
	private static Logger Base { get; } = CreateLogger("[{Timestamp:HH:mm:ss} {Level:u}] [{Category}] {Message:lj}{NewLine}{Exception}");
	
	private static Logger CreateLogger(string template) {
		return new LoggerConfiguration()
		       .MinimumLevel.Is(DefaultLogLevel.Value)
		       .MinimumLevel.Override("Microsoft", DefaultLogLevel.Coerce(LogEventLevel.Information))
		       .MinimumLevel.Override("Microsoft.AspNetCore", DefaultLogLevel.Coerce(LogEventLevel.Warning))
		       .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", DefaultLogLevel.Coerce(LogEventLevel.Warning))
		       .Filter.ByExcluding(static e => e.Exception is OperationCanceledException)
		       .Enrich.FromLogContext()
		       .WriteTo.Console(outputTemplate: template, formatProvider: CultureInfo.InvariantCulture, theme: AnsiConsoleTheme.Literate)
		       .CreateLogger();
	}

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
