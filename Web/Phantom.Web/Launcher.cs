using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Phantom.Server.Database;
using Phantom.Server.Web.Base;
using Phantom.Server.Web.Components.Utils;
using Phantom.Server.Web.Identity;
using Phantom.Server.Web.Identity.Interfaces;
using Serilog;

namespace Phantom.Server.Web;

public static class Launcher {
	public static async Task<WebApplication> CreateApplication(Configuration config, IConfigurator configurator, Action<DbContextOptionsBuilder> dbOptionsBuilder) {
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

		configurator.ConfigureServices(builder.Services);

		builder.Services.AddSingleton<IHostLifetime>(new NullLifetime());
		builder.Services.AddScoped<INavigation>(Navigation.Create(config.BasePath));

		builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(config.KeyFolderPath));

		builder.Services.AddDbContext<ApplicationDbContext>(dbOptionsBuilder, ServiceLifetime.Transient);
		builder.Services.AddSingleton<DatabaseProvider>();

		builder.Services.AddPhantomIdentity(config.CancellationToken);
		builder.Services.AddScoped<ILoginEvents, LoginEvents>();

		builder.Services.AddRazorPages(static options => options.RootDirectory = "/Layout");
		builder.Services.AddServerSideBlazor();

		var application = builder.Build();

		await MigrateDatabase(config, application.Services.GetRequiredService<DatabaseProvider>());
		await PhantomIdentityConfigurator.MigrateDatabase(application.Services);
		await configurator.LoadFromDatabase(application.Services);

		return application;
	}

	public static async Task Launch(Configuration config, WebApplication application) {
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

	private static async Task MigrateDatabase(Configuration config, DatabaseProvider databaseProvider) {
		var logger = config.Logger;

		using var scope = databaseProvider.CreateScope();
		var database = scope.Ctx.Database;

		logger.Information("Connecting to database...");

		var retryConnection = new Throttler(TimeSpan.FromSeconds(10));
		while (!await database.CanConnectAsync(config.CancellationToken)) {
			logger.Warning("Cannot connect to database, retrying...");
			await retryConnection.Wait();
		}

		logger.Information("Running database migrations...");
		await database.MigrateAsync(); // Do not allow cancellation.
	}

	public interface IConfigurator {
		void ConfigureServices(IServiceCollection services);
		Task LoadFromDatabase(IServiceProvider serviceProvider);
	}
}
