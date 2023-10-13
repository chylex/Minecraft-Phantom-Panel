using Phantom.Common.Data.Web.Users;
using Phantom.Common.Logging;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Web.Services.Rpc;
using ILogger = Serilog.ILogger;

namespace Phantom.Web.Services.Authentication;

public sealed class UserLoginManager {
	private static readonly ILogger Logger = PhantomLogger.Create<UserLoginManager>();

	private readonly INavigation navigation;
	private readonly UserSessionManager sessionManager;
	private readonly UserSessionBrowserStorage sessionBrowserStorage;
	private readonly CustomAuthenticationStateProvider authenticationStateProvider;
	private readonly ControllerConnection controllerConnection;

	public UserLoginManager(INavigation navigation, UserSessionManager sessionManager, UserSessionBrowserStorage sessionBrowserStorage, CustomAuthenticationStateProvider authenticationStateProvider, ControllerConnection controllerConnection) {
		this.navigation = navigation;
		this.sessionManager = sessionManager;
		this.sessionBrowserStorage = sessionBrowserStorage;
		this.authenticationStateProvider = authenticationStateProvider;
		this.controllerConnection = controllerConnection;
	}

	public async Task<bool> LogIn(string username, string password, string? returnUrl = null) {
		LogInSuccess? success;
		try {
			success = await controllerConnection.Send<LogInMessage, LogInSuccess?>(new LogInMessage(username, password), TimeSpan.FromSeconds(30));
		} catch (Exception e) {
			Logger.Error(e, "Could not log in {Username}.", username);
			return false;
		}

		if (success == null) {
			return false;
		}

		Logger.Information("Successfully logged in {Username}.", username);

		var userGuid = success.UserGuid;
		var userInfo = new UserInfo(userGuid, username, success.Permissions);
		var token = success.Token;

		await sessionBrowserStorage.Store(userGuid, token);
		sessionManager.Add(userInfo, token);
		
		authenticationStateProvider.SetLoadedSession(userInfo);
		await navigation.NavigateTo(returnUrl ?? string.Empty);
		
		return true;
	}

	public async Task LogOut() {
		var stored = await sessionBrowserStorage.Delete();
		if (stored != null) {
			sessionManager.Remove(stored.UserGuid, stored.Token);
		}

		await navigation.NavigateTo(string.Empty);
		authenticationStateProvider.SetUnloadedSession();
	}
}
