using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Phantom.Server.Web.Identity.Authentication;
using Phantom.Server.Web.Identity.Interfaces;

namespace Phantom.Server.Web.Identity;

sealed class PhantomIdentityMiddleware {
	public const string LoginPath = "/login";
	public const string LogoutPath = "/logout";

	public static bool AcceptsPath(HttpContext context) {
		var path = context.Request.Path;
		return path == LoginPath || path == LogoutPath;
	}
	
	private readonly RequestDelegate next;

	public PhantomIdentityMiddleware(RequestDelegate next) {
		this.next = next;
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public async Task InvokeAsync(HttpContext context, INavigation navigation, PhantomLoginManager loginManager) {
		var path = context.Request.Path;
		if (path == LoginPath && context.Request.Query.TryGetValue("token", out var tokens) && tokens[0] is {} token && await loginManager.ProcessToken(token) is {} result) {
			await context.SignInAsync(result.ClaimsPrincipal, result.AuthenticationProperties);
			context.Response.Redirect(navigation.BasePath + result.ReturnUrl);
		}
		else if (path == LogoutPath) {
			loginManager.OnSignedOut(context.User);
			await context.SignOutAsync();
			context.Response.Redirect(navigation.BasePath);
		}
		else {
			await next.Invoke(context);
		}
	}
}
