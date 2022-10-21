using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Phantom.Server.Database;
using Phantom.Server.Web.Identity.Authentication;
using Phantom.Server.Web.Identity.Authorization;
using Phantom.Server.Web.Identity.Data;

namespace Phantom.Server.Web.Identity;

public static class PhantomIdentityExtensions {
	public static void AddPhantomIdentity<TUser, TRole>(this IServiceCollection services) where TUser : class where TRole : class {
		services.AddIdentity<TUser, TRole>(ConfigureIdentity).AddEntityFrameworkStores<ApplicationDbContext>();
		services.ConfigureApplicationCookie(ConfigureIdentityCookie);
		services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);
		services.AddAuthorization(ConfigureAuthorization);

		services.AddSingleton<PhantomLoginStore>();
		services.AddScoped<PhantomLoginManager>();
		
		services.AddScoped<PhantomIdentityConfigurator>();
		services.AddScoped<IAuthorizationHandler, PermissionBasedPolicyHandler>();
		services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<TUser>>();
		
		services.AddTransient<PermissionManager>();
	}

	public static void UsePhantomIdentity(this IApplicationBuilder application) {
		application.UseAuthentication();
		application.UseAuthorization();
		application.UseMiddleware<PhantomIdentityMiddleware>();
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

	private static void ConfigureAuthorization(AuthorizationOptions o) {
		foreach (var permission in Permission.All) {
			o.AddPolicy(permission.Id, policy => policy.Requirements.Add(new PermissionBasedPolicyRequirement(permission)));
		}
	}
}
