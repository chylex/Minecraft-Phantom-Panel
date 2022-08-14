using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Phantom.Server.Web.Areas.Identity;
using Phantom.Server.Web.Database;
using Serilog;

namespace Phantom.Server.Web;

public static class Launcher {
	public static async Task Launch(Configuration config, Action<DbContextOptionsBuilder> dbOptionsBuilder) {
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
			ApplicationName = typeof(Launcher).Assembly.GetName().Name
		});

		builder.Host.UseSerilog(config.Logger, dispose: true);
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
		builder.Services.AddRazorPages();
		builder.Services.AddServerSideBlazor();
		builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();

		var app = builder.Build();

		app.UseSerilogRequestLogging();

		using (var scope = app.Services.CreateScope()) {
			await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
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

		config.Logger.Information("Starting Web server on port {Port}...", config.Port);
		await app.RunAsync(config.CancellationToken);
	}

	private static void ConfigureAuthentication(AuthenticationOptions o) {
		o.DefaultScheme = IdentityConstants.ApplicationScheme;
		o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
	}

	private static void ConfigureIdentity(IdentityOptions o) {
		o.Stores.MaxLengthForKeys = 128;
		o.SignIn.RequireConfirmedAccount = true;
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
