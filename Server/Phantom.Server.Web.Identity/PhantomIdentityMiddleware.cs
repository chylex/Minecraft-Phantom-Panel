using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Phantom.Server.Web.Identity.Authentication;

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
		if (path == LoginPath && context.Request.Query.TryGetValue("token", out var tokens) && tokens[0] is {} token && await loginManager.ProcessTokenAndGetReturnUrl(token) is {} returnUrl) {
			context.Response.Redirect(navigation.BasePath + returnUrl);
		}
		else if (path == LogoutPath) {
			await loginManager.SignOut();
			context.Response.Redirect(navigation.BasePath);
		}
		else {
			await next.Invoke(context);
		}
	}
}
