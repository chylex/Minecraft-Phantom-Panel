using Microsoft.AspNetCore.DataProtection;
using Phantom.Common.Messages.Web;
using Phantom.Utils.Rpc.Runtime;
using Phantom.Utils.Tasks;
using Phantom.Web.Base;
using Phantom.Web.Services;
using Serilog;

namespace Phantom.Web;

static class WebLauncher {
	public static WebApplication CreateApplication(Configuration config, TaskManager taskManager, ServiceConfiguration serviceConfiguration, RpcConnectionToServer<IMessageToControllerListener> controllerConnection) {
		var assembly = typeof(WebLauncher).Assembly;
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
			ApplicationName = assembly.GetName().Name,
			ContentRootPath = Path.GetDirectoryName(assembly.Location)
		});

		builder.Host.UseSerilog(config.Logger, dispose: true);

		builder.WebHost.UseUrls(config.HttpUrl);
		builder.WebHost.UseSetting(WebHostDefaults.DetailedErrorsKey, builder.Environment.IsDevelopment() ? "true" : "false");

		if (builder.Environment.IsEnvironment("Local")) {
			builder.WebHost.UseStaticWebAssets();
		}

		builder.Services.AddSingleton(taskManager);
		builder.Services.AddSingleton(serviceConfiguration);
		builder.Services.AddSingleton(controllerConnection);
		builder.Services.AddPhantomServices();

		builder.Services.AddSingleton<IHostLifetime>(new NullLifetime());
		builder.Services.AddScoped<INavigation>(Navigation.Create(config.BasePath));

		builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(config.DataProtectionKeyFolderPath));

		builder.Services.AddRazorPages(static options => options.RootDirectory = "/Layout");
		builder.Services.AddServerSideBlazor();

		return builder.Build();
	}

	public static Task Launch(Configuration config, WebApplication application) {
		var logger = config.Logger;

		application.UseSerilogRequestLogging();
		application.UsePathBase(config.BasePath);

		if (!application.Environment.IsDevelopment()) {
			application.UseExceptionHandler("/_Error");
		}

		application.UseStaticFiles();
		application.UseRouting();
		application.UsePhantomServices();

		application.MapControllers();
		application.MapBlazorHub();
		application.MapFallbackToPage("/_Host");

		logger.Information("Starting Web server on port {Port}...", config.Port);
		return application.RunAsync(config.CancellationToken);
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
