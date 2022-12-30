using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Phantom.Common.Logging;
using Phantom.Server.Web.Identity.Interfaces;
using Phantom.Utils.Cryptography;
using ILogger = Serilog.ILogger;

namespace Phantom.Server.Web.Identity.Authentication;

public sealed class PhantomLoginManager {
	private static readonly ILogger Logger = PhantomLogger.Create<PhantomLoginManager>();

	public static bool IsAuthenticated(ClaimsPrincipal user) {
		return user.Identity is { IsAuthenticated: true };
	}
	
	internal static string? GetAuthenticatedUserId(ClaimsPrincipal user, UserManager<IdentityUser> userManager) {
		return IsAuthenticated(user) ? userManager.GetUserId(user) : null;
	}
	
	private readonly INavigation navigation;
	private readonly PhantomLoginStore loginStore;
	private readonly UserManager<IdentityUser> userManager;
	private readonly SignInManager<IdentityUser> signInManager;
	private readonly ILoginEvents loginEvents;

	public PhantomLoginManager(INavigation navigation, PhantomLoginStore loginStore, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ILoginEvents loginEvents) {
		this.navigation = navigation;
		this.loginStore = loginStore;
		this.userManager = userManager;
		this.signInManager = signInManager;
		this.loginEvents = loginEvents;
	}

	public async Task<SignInResult> SignIn(string username, string password, string? returnUrl = null) {
		var user = await userManager.FindByNameAsync(username);
		if (user == null) {
			return SignInResult.Failed;
		}
		
		var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
		if (result == SignInResult.Success) {
			Logger.Verbose("Created login token for {Username}.", username);
			
			string token = TokenGenerator.Create(60);
			loginStore.Add(token, user, password, returnUrl ?? string.Empty);
			navigation.NavigateTo("login" + QueryString.Create("token", token), forceLoad: true);
		}
		
		return result;
	}

	internal async Task SignOut() {
		if (GetAuthenticatedUserId(signInManager.Context.User, userManager) is {} userId) {
			loginEvents.UserLoggedOut(userId);
		}

		await signInManager.SignOutAsync();
	}

	internal async Task<string?> ProcessTokenAndGetReturnUrl(string token) {
		var entry = loginStore.Pop(token);
		if (entry == null) {
			return null;
		}

		var user = entry.User;
		var result = await signInManager.PasswordSignInAsync(user, entry.Password, lockoutOnFailure: false, isPersistent: true);
		if (result == SignInResult.Success) {
			Logger.Information("Successful login for {Username}.", user.UserName);
			loginEvents.UserLoggedIn(user.Id);
			return entry.ReturnUrl;
		}
		else {
			Logger.Warning("Error logging in {Username}: {Result}.", user.UserName, result);
			return null;
		}
	}
}
