using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Phantom.Common.Data.Web.Users;
using Phantom.Web.Services.Agents;
using Phantom.Web.Services.Authentication;
using Phantom.Web.Services.Authorization;
using Phantom.Web.Services.Events;
using Phantom.Web.Services.Instances;
using Phantom.Web.Services.Rpc;
using Phantom.Web.Services.Users;

namespace Phantom.Web.Services;

public static class PhantomWebServices {
	public static void AddPhantomServices(this IServiceCollection services) {
		services.AddSingleton<ControllerConnection>();
		services.AddSingleton<ControllerMessageHandlerFactory>();
		
		services.AddSingleton<AgentManager>();
		services.AddSingleton<InstanceManager>();
		services.AddSingleton<InstanceLogManager>();
		services.AddSingleton<EventLogManager>();
		
		services.AddSingleton<UserManager>();
		services.AddSingleton<AuditLogManager>();
		services.AddSingleton<UserSessionRefreshManager>();
		services.AddScoped<UserLoginManager>();
		services.AddScoped<UserSessionBrowserStorage>();
		
		services.AddSingleton<RoleManager>();
		services.AddSingleton<UserRoleManager>();
		
		services.AddScoped<CustomAuthenticationStateProvider>();
		services.AddScoped<AuthenticationStateProvider>(static services => services.GetRequiredService<CustomAuthenticationStateProvider>());
		
		services.AddAuthorization(ConfigureAuthorization);
		services.AddScoped<IAuthorizationHandler, PermissionBasedPolicyHandler>();
	}
	
	public static void UsePhantomServices(this IApplicationBuilder application) {
		application.UseAuthorization();
	}

	private static void ConfigureAuthorization(AuthorizationOptions o) {
		foreach (var permission in Permission.All) {
			o.AddPolicy(permission.Id, policy => policy.Requirements.Add(new PermissionBasedPolicyRequirement(permission)));
		}
	}
}
