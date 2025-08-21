using System.Diagnostics.CodeAnalysis;
using Akka.Actor;
using Akka.Dispatch;
using Akka.Event;
using Phantom.Utils.Logging;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Events;
using LogEvent = Akka.Event.LogEvent;

namespace Phantom.Utils.Actor.Logging;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class SerilogLogger : ReceiveActor, IRequiresMessageQueue<ILoggerMessageQueueSemantics> {
	private readonly Dictionary<string, ILogger> loggersBySource = new ();
	
	public SerilogLogger() {
		Receive<InitializeLogger>(Initialize);
		
		Receive<Debug>(LogDebug);
		Receive<Info>(LogInfo);
		Receive<Warning>(LogWarning);
		Receive<Error>(LogError);
	}
	
	private void Initialize(InitializeLogger message) {
		Sender.Tell(new LoggerInitialized());
	}
	
	private void LogDebug(Debug item) {
		Log(item, LogEventLevel.Debug);
	}
	
	private void LogInfo(Info item) {
		Log(item, LogEventLevel.Information);
	}
	
	private void LogWarning(Warning item) {
		Log(item, LogEventLevel.Warning);
	}
	
	private void LogError(Error item) {
		Log(item, LogEventLevel.Error);
	}
	
	private void Log(LogEvent item, LogEventLevel level) {
		GetLogger(item).Write(level, item.Cause, GetFormat(item), GetArgs(item));
	}
	
	private ILogger GetLogger(LogEvent item) {
		var source = item.LogSource;
		
		if (!loggersBySource.TryGetValue(source, out var logger)) {
			var loggerName = source[(source.IndexOf(':') + 1)..];
			loggersBySource[source] = logger = PhantomLogger.Create("Akka", loggerName);
		}
		
		return logger;
	}
	
	private static string GetFormat(LogEvent item) {
		return item.Message is LogMessage logMessage ? logMessage.Format : "{Message:l}";
	}
	
	private static object[] GetArgs(LogEvent item) {
		return item.Message is LogMessage logMessage ? logMessage.Parameters().Where(static a => a is not PropertyEnricher).ToArray() : new[] { item.Message };
	}
}
