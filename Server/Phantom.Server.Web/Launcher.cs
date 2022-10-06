using Serilog;

namespace Phantom.Server.Web;

public static class Launcher {
	public static WebApplication CreateApplication(Configuration config, IConfigurator configurator) {
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
			ApplicationName = typeof(Launcher).Assembly.GetName().Name
		});

		builder.Host.UseSerilog(config.Logger, dispose: true);
		
		builder.WebHost.UseUrls(config.HttpUrl);
		builder.WebHost.UseSetting(WebHostDefaults.DetailedErrorsKey, builder.Environment.IsDevelopment() ? "true" : "false");

		if (builder.Environment.IsEnvironment("Local")) {
			builder.WebHost.UseStaticWebAssets();
		}

		configurator.ConfigureServices(builder.Services);
		
		builder.Services.AddSingleton<IHostLifetime>(new NullLifetime());
		
		builder.Services.AddRazorPages(static options => options.RootDirectory = "/Layout");
		builder.Services.AddServerSideBlazor();

		return builder.Build();
	}
	
	public static async Task Launch(Configuration config, WebApplication application) {
		var logger = config.Logger;

		application.UseSerilogRequestLogging();

		if (!application.Environment.IsDevelopment()) {
			application.UseExceptionHandler("/_Error");
		}

		application.UseStaticFiles();
		application.UseRouting();
		application.UseAuthentication();
		application.UseAuthorization();

		application.MapControllers();
		application.MapBlazorHub();
		application.MapFallbackToPage("/_Host");

		logger.Information("Starting Web server on port {Port}...", config.Port);
		await application.RunAsync(config.CancellationToken);
	}

	private sealed class NullLifetime : IHostLifetime {
		public Task WaitForStartAsync(CancellationToken cancellationToken) {
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken) {
			return Task.CompletedTask;
		}
	}

	public interface IConfigurator {
		void ConfigureServices(IServiceCollection services);
	}
}
