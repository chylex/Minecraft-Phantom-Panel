using Microsoft.AspNetCore.DataProtection;
using Phantom.Controller.Services;
using Phantom.Utils.Tasks;
using Phantom.Web.Base;
using Phantom.Web.Identity;
using Phantom.Web.Identity.Interfaces;
using Serilog;

namespace Phantom.Web;

public static class Launcher {
	public static WebApplication CreateApplication(Configuration config, ServiceConfiguration serviceConfiguration, TaskManager taskManager) {
		var assembly = typeof(Launcher).Assembly;
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

		builder.Services.AddSingleton(serviceConfiguration);
		builder.Services.AddSingleton(taskManager);

		builder.Services.AddSingleton<IHostLifetime>(new NullLifetime());
		builder.Services.AddScoped<INavigation>(Navigation.Create(config.BasePath));

		builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(config.KeyFolderPath));

		builder.Services.AddPhantomIdentity(config.CancellationToken);
		builder.Services.AddScoped<ILoginEvents, LoginEvents>();

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
		application.UsePhantomIdentity();

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
