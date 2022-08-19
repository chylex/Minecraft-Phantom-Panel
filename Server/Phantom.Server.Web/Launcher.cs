using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Phantom.Common.Logging;
using Phantom.Server.Web.Areas.Identity;
using Phantom.Server.Web.Database;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Phantom.Server.Web;

public static class Launcher {
	public static async Task Launch(Configuration config, Action<DbContextOptionsBuilder> dbOptionsBuilder) {
		var logger = config.Logger;
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
			ApplicationName = typeof(Launcher).Assembly.GetName().Name
		});

		builder.Host.UseSerilog(logger, dispose: true);
		builder.Host.ConfigureServices(static services => services.AddSingleton<IHostLifetime>(new NullLifetime()));

		builder.WebHost.UseUrls(config.HttpUrl);
		builder.WebHost.UseSetting(WebHostDefaults.DetailedErrorsKey, builder.Environment.IsDevelopment() ? "true" : "false");

		if (builder.Environment.IsEnvironment("Local")) {
			builder.WebHost.UseStaticWebAssets();
		}

		builder.Services.AddDbContext<ApplicationDbContext>(dbOptionsBuilder);
		builder.Services.AddDatabaseDeveloperPageExceptionFilter();

		builder.Services.AddAuthentication(ConfigureAuthentication).AddIdentityCookies(static _ => {});
		builder.Services.AddIdentityCore<IdentityUser>(ConfigureIdentity).AddDefaultTokenProviders().AddEntityFrameworkStores<ApplicationDbContext>();
		builder.Services.AddRazorPages(static options => options.RootDirectory = "/Layout");
		builder.Services.AddServerSideBlazor();
		builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();

		var app = builder.Build();

		app.UseSerilogRequestLogging();

		using (var scope = app.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database;

			logger.Information("Connecting to database...");
			await WaitForDatabaseConnection(logger, db);

			logger.Information("Running database migrations...");
			await db.MigrateAsync();
		}

		if (app.Environment.IsDevelopment()) {
			app.UseMigrationsEndPoint();
		}
		else {
			app.UseExceptionHandler("/Error");
		}

		app.UseStaticFiles();
		app.UseRouting();
		app.UseAuthentication();
		app.UseAuthorization();

		app.MapControllers();
		app.MapBlazorHub();
		app.MapFallbackToPage("/_Host");

		logger.Information("Starting Web server on port {Port}...", config.Port);
		await app.RunAsync(config.CancellationToken);
	}

	private sealed class NullLifetime : IHostLifetime {
		public Task WaitForStartAsync(CancellationToken cancellationToken) {
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken) {
			return Task.CompletedTask;
		}
	}

	private static void ConfigureAuthentication(AuthenticationOptions o) {
		o.DefaultScheme = IdentityConstants.ApplicationScheme;
		o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
	}

	private static void ConfigureIdentity(IdentityOptions o) {
		o.Stores.MaxLengthForKeys = 128;
		o.SignIn.RequireConfirmedAccount = true;
	}

	private static async Task WaitForDatabaseConnection(ILogger logger, DatabaseFacade db) {
		var retry = new Throttler(TimeSpan.FromSeconds(15));

		while (!await db.CanConnectAsync()) {
			logger.Warning("Cannot connect to database, retrying...");
			await retry.Wait();
		}
	}
}
