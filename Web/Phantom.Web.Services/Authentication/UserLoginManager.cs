using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Utils.Logging;
using Phantom.Web.Services.Rpc;
using ILogger = Serilog.ILogger;

namespace Phantom.Web.Services.Authentication;

public sealed class UserLoginManager {
	private static readonly ILogger Logger = PhantomLogger.Create<UserLoginManager>();

	private readonly Navigation navigation;
	private readonly UserSessionBrowserStorage sessionBrowserStorage;
	private readonly CustomAuthenticationStateProvider authenticationStateProvider;
	private readonly ControllerConnection controllerConnection;

	public UserLoginManager(Navigation navigation, UserSessionBrowserStorage sessionBrowserStorage, CustomAuthenticationStateProvider authenticationStateProvider, ControllerConnection controllerConnection) {
		this.navigation = navigation;
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

		var userInfo = success.UserInfo;
		var authToken = success.AuthToken;
		
		authenticationStateProvider.SetUnloadedSession();
		await sessionBrowserStorage.Store(userInfo.Guid, authToken);
		await authenticationStateProvider.GetAuthenticationStateAsync();
		await navigation.NavigateTo(returnUrl ?? string.Empty);
		
		return true;
	}

	public async Task LogOut() {
		var stored = await sessionBrowserStorage.Delete();
		if (stored != null) {
			await controllerConnection.Send(new LogOutMessage(stored.UserGuid, stored.Token));
		}

		await navigation.NavigateTo(string.Empty);
		authenticationStateProvider.SetUnloadedSession();
	}
}
