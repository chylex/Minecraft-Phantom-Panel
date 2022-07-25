using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Phantom.Server.Web.Areas.Identity;
using Phantom.Server.Web.Data;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Phantom.Server.Web;

public static class Launcher {
	public static async Task Launch(ILogger logger, ushort port, Action<DbContextOptionsBuilder> dbOptionsBuilder) {
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
			ApplicationName = typeof(Launcher).Assembly.GetName().Name
		});

		builder.Host.UseSerilog(logger, dispose: true);

		builder.WebHost.UseUrls("http://0.0.0.0:" + port);
		builder.WebHost.UseSetting(WebHostDefaults.DetailedErrorsKey, builder.Environment.IsDevelopment() ? "true" : "false");

		builder.Services.AddDbContext<ApplicationDbContext>(dbOptionsBuilder);
		builder.Services.AddDatabaseDeveloperPageExceptionFilter();

		static void ConfigureAuthentication(AuthenticationOptions o) {
			o.DefaultScheme = IdentityConstants.ApplicationScheme;
			o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
		}

		static void ConfigureIdentity(IdentityOptions o) {
			o.Stores.MaxLengthForKeys = 128;
			o.SignIn.RequireConfirmedAccount = true;
		}

		builder.Services.AddAuthentication(ConfigureAuthentication).AddIdentityCookies(static _ => {});
		builder.Services.AddIdentityCore<IdentityUser>(ConfigureIdentity).AddDefaultTokenProviders().AddEntityFrameworkStores<ApplicationDbContext>();
		builder.Services.AddRazorPages();
		builder.Services.AddServerSideBlazor();
		builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
		builder.Services.AddSingleton<WeatherForecastService>();

		var app = builder.Build();
		var environment = app.Environment;

		app.UseSerilogRequestLogging();

		using (var scope = app.Services.CreateScope()) {
			await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
		}

		if (environment.IsDevelopment()) {
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

		logger.Information("Starting Web server on port {Port}...", port);
		await app.RunAsync();
	}
}
