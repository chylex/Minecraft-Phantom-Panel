using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace Phantom.Web.Services.Authentication;

public sealed class CustomAuthenticationStateProvider : ServerAuthenticationStateProvider {
	private readonly UserSessionManager sessionManager;
	private readonly UserSessionBrowserStorage sessionBrowserStorage;
	private bool isLoaded;

	public CustomAuthenticationStateProvider(UserSessionManager sessionManager, UserSessionBrowserStorage sessionBrowserStorage) {
		this.sessionManager = sessionManager;
		this.sessionBrowserStorage = sessionBrowserStorage;
	}

	public override async Task<AuthenticationState> GetAuthenticationStateAsync() {
		if (!isLoaded) {
			var stored = await sessionBrowserStorage.Get();
			if (stored != null) {
				var session = sessionManager.FindWithToken(stored.UserGuid, stored.Token);
				if (session != null) {
					SetLoadedSession(session);
				}
			}
		}

		return await base.GetAuthenticationStateAsync();
	}

	internal void SetLoadedSession(UserInfo user) {
		isLoaded = true;
		SetAuthenticationState(Task.FromResult(new AuthenticationState(user.AsClaimsPrincipal)));
	}

	internal void SetUnloadedSession() {
		isLoaded = false;
		SetAuthenticationState(Task.FromResult(new AuthenticationState(new ClaimsPrincipal())));
	}
}
