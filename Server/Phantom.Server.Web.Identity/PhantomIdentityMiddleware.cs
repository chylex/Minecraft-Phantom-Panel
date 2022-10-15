using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Phantom.Server.Web.Identity.Authentication;

namespace Phantom.Server.Web.Identity;

sealed class PhantomIdentityMiddleware {
	private readonly RequestDelegate next;

	public PhantomIdentityMiddleware(RequestDelegate next) {
		this.next = next;
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public async Task InvokeAsync(HttpContext context, INavigation navigation, PhantomLoginManager loginManager) {
		var path = context.Request.Path;
		if (path == "/login" && context.Request.Query.TryGetValue("token", out var tokens) && tokens[0] is {} token && await loginManager.ProcessTokenAndGetReturnUrl(token) is {} returnUrl) {
			context.Response.Redirect(navigation.BasePath + returnUrl);
		}
		else if (path == "/logout") {
			await loginManager.SignOut();
			context.Response.Redirect(navigation.BasePath);
		}
		else {
			await next.Invoke(context);
		}
	}
}
