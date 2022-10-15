using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Phantom.Server.Web.Identity.Authentication;

namespace Phantom.Server.Web.Identity; 

public static class PhantomIdentityExtensions {
	public static void AddPhantomIdentity<TUser>(this IServiceCollection services) where TUser : class {
		services.AddSingleton<PhantomLoginStore>();
		services.AddScoped<PhantomLoginManager>();
		services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<TUser>>();
	}
	
	public static void UsePhantomIdentity(this IApplicationBuilder application) {
		application.UseMiddleware<PhantomIdentityMiddleware>();
	}
}
