using System.Diagnostics.CodeAnalysis;

namespace Phantom.Server.Web.Authentication;

sealed class BlazorIdentityMiddleware {
	private readonly RequestDelegate next;

	public BlazorIdentityMiddleware(RequestDelegate next) {
		this.next = next;
	}

	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public async Task InvokeAsync(HttpContext context, PhantomLoginManager loginManager) {
		var path = context.Request.Path;
		if (path == "/login" && context.Request.Query.TryGetValue("token", out var tokens) && tokens[0] is {} token && await loginManager.ProcessTokenAndGetReturnUrl(token) is {} returnUrl) {
			context.Response.Redirect(returnUrl);
		}
		else if (path == "/logout") {
			await loginManager.SignOut();
			context.Response.Redirect("/");
		}
		else {
			await next.Invoke(context);
		}
	}
}
