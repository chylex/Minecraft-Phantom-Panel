using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Phantom.Web.Identity.Authentication;
using Phantom.Web.Identity.Authorization;

namespace Phantom.Web.Identity;

public static class PhantomIdentityExtensions {
	public static void AddPhantomIdentity(this IServiceCollection services, CancellationToken cancellationToken) {
		services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(ConfigureIdentityCookie);
		services.AddAuthorization(ConfigureAuthorization);

		services.AddSingleton(PhantomLoginStore.Create(cancellationToken));
		services.AddScoped<PhantomLoginManager>();
		
		services.AddScoped<IAuthorizationHandler, PermissionBasedPolicyHandler>();
		services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
	}

	public static void UsePhantomIdentity(this IApplicationBuilder application) {
		application.UseAuthentication();
		application.UseAuthorization();
		application.UseWhen(PhantomIdentityMiddleware.AcceptsPath, static app => app.UseMiddleware<PhantomIdentityMiddleware>());
	}

	private static void ConfigureIdentityCookie(CookieAuthenticationOptions o) {
		o.Cookie.Name = "Phantom.Identity";
		o.Cookie.HttpOnly = true;
		o.Cookie.SameSite = SameSiteMode.Lax;

		o.ExpireTimeSpan = TimeSpan.FromDays(30);
		o.SlidingExpiration = true;

		o.LoginPath = PhantomIdentityMiddleware.LoginPath;
		o.LogoutPath = PhantomIdentityMiddleware.LogoutPath;
		o.AccessDeniedPath = PhantomIdentityMiddleware.LoginPath;
	}

	private static void ConfigureAuthorization(AuthorizationOptions o) {
		foreach (var permission in Permission.All) {
			o.AddPolicy(permission.Id, policy => policy.Requirements.Add(new PermissionBasedPolicyRequirement(permission)));
		}
	}
}
