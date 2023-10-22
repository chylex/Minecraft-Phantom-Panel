using System.Diagnostics.CodeAnalysis;
using Serilog.Events;

namespace Phantom.Common.Logging;

static class DefaultLogLevel {
	private const string ENVIRONMENT_VARIABLE = "LOG_LEVEL";
	
	public static LogEventLevel Value { get; } = GetDefaultLevel();
	
	public static LogEventLevel Coerce(LogEventLevel level) {
		return level < Value ? Value : level;
	}
	
	private static LogEventLevel GetDefaultLevel() {
		var level = Environment.GetEnvironmentVariable(ENVIRONMENT_VARIABLE);
		return level switch {
			"VERBOSE"     => LogEventLevel.Verbose,
			"DEBUG"       => LogEventLevel.Debug,
			"INFORMATION" => LogEventLevel.Information,
			"WARNING"     => LogEventLevel.Warning,
			"ERROR"       => LogEventLevel.Error,
			null          => GetDefaultLevelFallback(),
			_             => LogEnvironmentVariableErrorAndExit(level)
		};
	}

	private static LogEventLevel GetDefaultLevelFallback() {
		#if DEBUG
		return LogEventLevel.Verbose;
		#else
		return LogEventLevel.Information;
		#endif
	}

	[DoesNotReturn]
	private static LogEventLevel LogEnvironmentVariableErrorAndExit(string logLevel) {
		Console.Error.WriteLine("Invalid value of environment variable {0}: {1}", ENVIRONMENT_VARIABLE, logLevel);
		Environment.Exit(1);
		return LogEventLevel.Fatal;
	}
}
