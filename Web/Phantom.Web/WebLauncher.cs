using Microsoft.AspNetCore.DataProtection;
using Phantom.Common.Messages.Web;
using Phantom.Utils.Rpc.Message;
using Phantom.Web.Services;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Phantom.Web;

static class WebLauncher {
	internal sealed record Configuration(ILogger Logger, string Host, ushort Port, string BasePath, string DataProtectionKeyFolderPath, CancellationToken CancellationToken) {
		public string HttpUrl => "http://" + Host + ":" + Port;
	}
	
	internal static WebApplication CreateApplication(Configuration config, ApplicationProperties applicationProperties, MessageSender<IMessageToController> sendChannel) {
		var assembly = typeof(WebLauncher).Assembly;
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
			ApplicationName = assembly.GetName().Name,
			ContentRootPath = Path.GetDirectoryName(assembly.Location),
		});
		
		builder.Host.UseSerilog(config.Logger, dispose: true);
		
		builder.WebHost.UseUrls(config.HttpUrl);
		builder.WebHost.UseSetting(WebHostDefaults.DetailedErrorsKey, builder.Environment.IsDevelopment() ? "true" : "false");
		
		if (builder.Environment.IsEnvironment("Local")) {
			builder.WebHost.UseStaticWebAssets();
		}
		
		builder.Services.AddSingleton(applicationProperties);
		builder.Services.AddSingleton(sendChannel);
		builder.Services.AddPhantomServices();
		
		builder.Services.AddSingleton<IHostLifetime>(new NullLifetime());
		builder.Services.AddScoped(Navigation.Create(config.BasePath));
		
		builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(config.DataProtectionKeyFolderPath));
		
		builder.Services.AddRazorPages(static options => options.RootDirectory = "/Layout");
		builder.Services.AddServerSideBlazor();
		
		return builder.Build();
	}
	
	internal static Task Launch(Configuration config, WebApplication application) {
		var logger = config.Logger;
		
		application.UseSerilogRequestLogging();
		application.UsePathBase(config.BasePath);
		
		if (!application.Environment.IsDevelopment()) {
			application.UseExceptionHandler("/_Error");
		}
		
		application.UseRouting();
		application.UsePhantomServices();
		
		application.MapStaticAssets();
		application.MapControllers();
		application.MapBlazorHub();
		application.MapFallbackToPage("/_Host");
		
		logger.Information("Starting Web server on port {Port}...", config.Port);
		return application.StartAsync(config.CancellationToken);
	}
	
	private sealed class NullLifetime : IHostLifetime {
		public Task WaitForStartAsync(CancellationToken cancellationToken) {
			return Task.CompletedTask;
		}
		
		public Task StopAsync(CancellationToken cancellationToken) {
			return Task.CompletedTask;
		}
	}
}
