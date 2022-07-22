using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Phantom.Server.Web.Areas.Identity;
using Phantom.Server.Web.Data;

namespace Phantom.Server.Web; 

public static class Launcher {
	public static void Launch(Action<DbContextOptionsBuilder> dbOptionsBuilder) {
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
			ApplicationName = typeof(Launcher).Assembly.GetName().Name
		});

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

		app.Run();
	}
}
