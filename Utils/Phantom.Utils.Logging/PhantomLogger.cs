using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Phantom.Utils.Logging;

public static class PhantomLogger {
	public static Logger Base { get; } = CreateBaseLogger();

	private static Logger CreateBaseLogger() {
		var configuration = new LoggerConfiguration();
		
		#if DEBUG
		configuration.MinimumLevel.Is(LogEventLevel.Debug);
		#else
		configuration.MinimumLevel.Is(LogEventLevel.Information);
		#endif
		
		configuration.MinimumLevel.Override("Microsoft", LogEventLevel.Information);
		configuration.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);
		configuration.Enrich.FromLogContext();
		configuration.WriteTo.Console();
		
		return configuration.CreateLogger();
	}

	public static ILogger Create<T>() {
		return Base.ForContext<T>();
	}
}
