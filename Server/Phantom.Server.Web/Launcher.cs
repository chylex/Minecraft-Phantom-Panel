using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Phantom.Server.Database;
using Phantom.Server.Web.Components.Utils;
using Phantom.Server.Web.Identity;
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

		builder.Services.AddDbContextPool<ApplicationDbContext>(dbOptionsBuilder, poolSize: 64);
		builder.Services.AddSingleton<DatabaseProvider>();

		builder.Services.AddIdentity<IdentityUser, IdentityRole>(ConfigureIdentity).AddEntityFrameworkStores<ApplicationDbContext>();
		builder.Services.ConfigureApplicationCookie(ConfigureIdentityCookie);
		builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);
		builder.Services.AddAuthorization();
		builder.Services.AddPhantomIdentity<IdentityUser>();

		builder.Services.AddRazorPages(static options => options.RootDirectory = "/Layout");
		builder.Services.AddServerSideBlazor();

		var application = builder.Build();

		await MigrateDatabase(config, application.Services.GetRequiredService<DatabaseProvider>());
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
		application.UseAuthentication();
		application.UseAuthorization();
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

	private static void ConfigureIdentity(IdentityOptions o) {
		o.SignIn.RequireConfirmedAccount = false;
		o.SignIn.RequireConfirmedEmail = false;
		o.SignIn.RequireConfirmedPhoneNumber = false;

		o.Password.RequireLowercase = true;
		o.Password.RequireUppercase = true;
		o.Password.RequireDigit = true;
		o.Password.RequiredLength = 16;

		o.Lockout.AllowedForNewUsers = true;
		o.Lockout.MaxFailedAccessAttempts = 10;
		o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);

		o.Stores.MaxLengthForKeys = 128;
	}

	private static void ConfigureIdentityCookie(CookieAuthenticationOptions o) {
		o.Cookie.Name = "Phantom.Identity";
		o.Cookie.HttpOnly = true;
		o.Cookie.SameSite = SameSiteMode.Lax;

		o.ExpireTimeSpan = TimeSpan.FromDays(30);
		o.SlidingExpiration = true;

		o.LoginPath = "/login";
		o.LogoutPath = "/logout";
		o.AccessDeniedPath = "/login";
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
		void ConfigureServices( IServiceCollection services);
		Task LoadFromDatabase(IServiceProvider serviceProvider);
	}
}
