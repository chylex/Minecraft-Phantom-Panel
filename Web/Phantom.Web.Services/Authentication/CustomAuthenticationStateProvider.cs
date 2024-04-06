using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Web.Services.Rpc;

namespace Phantom.Web.Services.Authentication;

public sealed class CustomAuthenticationStateProvider : ServerAuthenticationStateProvider {
	private readonly UserSessionBrowserStorage sessionBrowserStorage;
	private readonly ControllerConnection controllerConnection;
	private bool isLoaded;

	public CustomAuthenticationStateProvider(UserSessionBrowserStorage sessionBrowserStorage, ControllerConnection controllerConnection) {
		this.sessionBrowserStorage = sessionBrowserStorage;
		this.controllerConnection = controllerConnection;
	}

	public override async Task<AuthenticationState> GetAuthenticationStateAsync() {
		if (!isLoaded) {
			var stored = await sessionBrowserStorage.Get();
			if (stored != null) {
				var authToken = stored.Token;
				var session = await controllerConnection.Send<GetAuthenticatedUser, Optional<AuthenticatedUserInfo>>(new GetAuthenticatedUser(stored.UserGuid, authToken), TimeSpan.FromSeconds(30));
				if (session.Value is {} userInfo) {
					SetLoadedSession(new AuthenticatedUser(userInfo, authToken));
				}
			}
		}

		return await base.GetAuthenticationStateAsync();
	}

	internal void SetLoadedSession(AuthenticatedUser authenticatedUser) {
		isLoaded = true;
		SetAuthenticationState(Task.FromResult(new AuthenticationState(new CustomClaimsPrincipal(authenticatedUser))));
	}

	internal void SetUnloadedSession() {
		isLoaded = false;
		SetAuthenticationState(Task.FromResult(new AuthenticationState(new ClaimsPrincipal())));
	}
}
