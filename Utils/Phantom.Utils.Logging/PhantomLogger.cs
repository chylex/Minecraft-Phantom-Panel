using System.Globalization;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Phantom.Utils.Logging;

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
		       .WriteTo.Async(c => c.Console(outputTemplate: template, formatProvider: CultureInfo.InvariantCulture, theme: AnsiConsoleTheme.Literate))
		       .CreateLogger();
	}
	
	public static ILogger Create(string name) {
		return Base.ForContext("Category", name);
	}
	
	public static ILogger Create(string name1, string name2) {
		return Create(ConcatNames(name1, name2));
	}
	
	public static ILogger Create<T>() {
		return Create(TypeName<T>());
	}
	
	public static ILogger Create<T>(string name) {
		return Create(ConcatNames(TypeName<T>(), name));
	}
	
	public static ILogger Create<T>(string name1, string name2) {
		return Create(ConcatNames(TypeName<T>(), name1, name2));
	}
	
	public static ILogger Create<T1, T2>() {
		return Create(ConcatNames(TypeName<T1>(), TypeName<T2>()));
	}
	
	public static ILogger Create<T1, T2>(string name) {
		return Create(ConcatNames(TypeName<T1>(), TypeName<T2>(), name));
	}
	
	public static ILogger Create<T1, T2>(string name1, string name2) {
		return Create(ConcatNames(TypeName<T1>(), TypeName<T2>(), ConcatNames(name1, name2)));
	}
	
	private static string TypeName<T>() {
		string typeName = typeof(T).Name;
		int genericsStartIndex = typeName.IndexOf('`');
		return genericsStartIndex > 0 ? typeName[..genericsStartIndex] : typeName;
	}
	
	public static string ConcatNames(string name1, string name2) {
		return name1 + ":" + name2;
	}
	
	public static string ConcatNames(string name1, string name2, string name3) {
		return ConcatNames(name1, ConcatNames(name2, name3));
	}
	
	public static string ShortenGuid(Guid guid) {
		var prefix = guid.ToString();
		return prefix[..prefix.IndexOf('-')];
	}
	
	public static void Dispose() {
		Root.Dispose();
		Base.Dispose();
	}
}
