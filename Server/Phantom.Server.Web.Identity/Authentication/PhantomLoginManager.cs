using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Phantom.Common.Logging;
using Phantom.Server.Services.Users;
using Phantom.Server.Web.Identity.Interfaces;
using Phantom.Utils.Cryptography;
using ILogger = Serilog.ILogger;

namespace Phantom.Server.Web.Identity.Authentication;

public sealed class PhantomLoginManager {
	private static readonly ILogger Logger = PhantomLogger.Create<PhantomLoginManager>();

	public static bool IsAuthenticated(ClaimsPrincipal user) {
		return user.Identity is { IsAuthenticated: true };
	}

	private readonly INavigation navigation;
	private readonly UserManager userManager;
	private readonly PhantomLoginStore loginStore;
	private readonly ILoginEvents loginEvents;

	public PhantomLoginManager(INavigation navigation, UserManager userManager, PhantomLoginStore loginStore, ILoginEvents loginEvents) {
		this.navigation = navigation;
		this.userManager = userManager;
		this.loginStore = loginStore;
		this.loginEvents = loginEvents;
	}

	public async Task<bool> SignIn(string username, string password, string? returnUrl = null) {
		if (await userManager.GetAuthenticated(username, password) == null) {
			return false;
		}
		
		Logger.Debug("Created login token for {Username}.", username);

		string token = TokenGenerator.Create(60);
		loginStore.Add(token, username, password, returnUrl ?? string.Empty);
		navigation.NavigateTo("login" + QueryString.Create("token", token), forceLoad: true);

		return true;
	}

	internal async Task<SignInResult?> ProcessToken(string token) {
		var entry = loginStore.Pop(token);
		if (entry == null) {
			return null;
		}

		var user = await userManager.GetAuthenticated(entry.Username, entry.Password);
		if (user == null) {
			return null;
		}

		Logger.Information("Successful login for {Username}.", user.Name);
		loginEvents.UserLoggedIn(user);

		var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
		identity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
		identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.UserGuid.ToString()));

		var authenticationProperties = new AuthenticationProperties {
			IsPersistent = true
		};
		
		return new SignInResult(new ClaimsPrincipal(identity), authenticationProperties, entry.ReturnUrl);
	}

	internal sealed record SignInResult(ClaimsPrincipal ClaimsPrincipal, AuthenticationProperties AuthenticationProperties, string ReturnUrl);

	internal void OnSignedOut(ClaimsPrincipal user) {
		if (UserManager.GetAuthenticatedUserId(user) is {} userGuid) {
			loginEvents.UserLoggedOut(userGuid);
		}
	}
}
