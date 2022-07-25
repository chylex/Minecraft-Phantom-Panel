using Serilog;
using ILogger = Serilog.ILogger;

namespace Phantom.Server.Grpc;

public static class Launcher {
	public static async Task Launch(ILogger logger, ushort port) {
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
			ApplicationName = typeof(Launcher).Assembly.GetName().Name
		});

		builder.Host.UseSerilog(logger, dispose: true);

		builder.WebHost.UseUrls("https://0.0.0.0:" + port);
		builder.WebHost.UseSetting(WebHostDefaults.DetailedErrorsKey, builder.Environment.IsDevelopment() ? "true" : "false");

		var app = builder.Build();

		app.UseSerilogRequestLogging();

		app.UseRouting();
		app.UseEndpoints(static endpoints => {
			// endpoints.MapGrpcService<>()
		});

		logger.Information("Starting GRPC server on port {Port}...", port);
		await app.RunAsync();
	}
}
