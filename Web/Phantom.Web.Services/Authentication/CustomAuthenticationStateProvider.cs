using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Utils.Logging;
using Phantom.Web.Services.Rpc;
using ILogger = Serilog.ILogger;

namespace Phantom.Web.Services.Authentication;

public sealed class CustomAuthenticationStateProvider : ServerAuthenticationStateProvider, IAsyncDisposable {
	private static readonly ILogger Logger = PhantomLogger.Create<CustomAuthenticationStateProvider>();
	
	private readonly UserSessionRefreshManager sessionRefreshManager;
	private readonly UserSessionBrowserStorage sessionBrowserStorage;
	private readonly ControllerConnection controllerConnection;
	
	private readonly SemaphoreSlim loadSemaphore = new (1);
	private bool isLoaded = false;
	private CancellationTokenSource? loadCancellationTokenSource;
	private UserSessionRefreshManager.EventHolder? userRefreshEventHolder;
	
	public CustomAuthenticationStateProvider(UserSessionRefreshManager sessionRefreshManager, UserSessionBrowserStorage sessionBrowserStorage, ControllerConnection controllerConnection) {
		this.sessionRefreshManager = sessionRefreshManager;
		this.sessionBrowserStorage = sessionBrowserStorage;
		this.controllerConnection = controllerConnection;
	}
	
	public override async Task<AuthenticationState> GetAuthenticationStateAsync() {
		if (!isLoaded) {
			await LoadSession();
		}
		
		return await base.GetAuthenticationStateAsync();
	}
	
	private async Task LoadSession() {
		await CancelCurrentLoad();
		await loadSemaphore.WaitAsync(CancellationToken.None);
		
		loadCancellationTokenSource = new CancellationTokenSource();
		CancellationToken cancellationToken = loadCancellationTokenSource.Token;
		
		try {
			var authenticatedUser = await TryGetSession(cancellationToken);
			if (authenticatedUser != null) {
				SetLoadedSession(authenticatedUser);
			}
			else {
				SetUnloadedSession();
			}
		} catch (OperationCanceledException) {
			SetUnloadedSession();
		} catch (Exception e) {
			SetUnloadedSession();
			Logger.Error(e, "Could not load user session.");
		} finally {
			loadCancellationTokenSource.Dispose();
			loadCancellationTokenSource = null;
			loadSemaphore.Release();
		}
	}
	
	private async Task CancelCurrentLoad() {
		var cancellationTokenSource = loadCancellationTokenSource;
		if (cancellationTokenSource != null) {
			await cancellationTokenSource.CancelAsync();
		}
	}
	
	private async Task<AuthenticatedUser?> TryGetSession(CancellationToken cancellationToken) {
		var stored = await sessionBrowserStorage.Get();
		if (stored == null) {
			return null;
		}
		
		cancellationToken.ThrowIfCancellationRequested();
		
		var userGuid = stored.UserGuid;
		var authToken = stored.Token;
		
		if (userRefreshEventHolder == null) {
			userRefreshEventHolder = sessionRefreshManager.GetEventHolder(userGuid);
			userRefreshEventHolder.UserNeedsRefresh += OnUserNeedsRefresh;
		}
		
		var session = await controllerConnection.Send<GetAuthenticatedUser, Optional<AuthenticatedUserInfo>>(new GetAuthenticatedUser(userGuid, authToken), TimeSpan.FromSeconds(30), cancellationToken);
		if (session.Value is {} userInfo) {
			return new AuthenticatedUser(userInfo, authToken);
		}
		else {
			return null;
		}
	}
	
	private void SetLoadedSession(AuthenticatedUser authenticatedUser) {
		SetAuthenticationState(Task.FromResult(new AuthenticationState(new CustomClaimsPrincipal(authenticatedUser))));
		isLoaded = true;
	}
	
	internal void SetUnloadedSession() {
		SetAuthenticationState(Task.FromResult(new AuthenticationState(new ClaimsPrincipal())));
		isLoaded = false;
	}
	
	private void OnUserNeedsRefresh(object? sender, EventArgs args) {
		_ = LoadSession();
	}
	
	public async ValueTask DisposeAsync() {
		if (userRefreshEventHolder != null) {
			userRefreshEventHolder.UserNeedsRefresh -= OnUserNeedsRefresh;
			userRefreshEventHolder = null;
		}
		
		await CancelCurrentLoad();
		loadSemaphore.Dispose();
	}
}
