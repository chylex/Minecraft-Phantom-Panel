using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Phantom.Common.Logging;
using Phantom.Utils.Cryptography;
using Serilog;

namespace Phantom.Server.Web.Identity.Authentication;

public sealed class PhantomLoginManager {
	private static readonly ILogger Logger = PhantomLogger.Create<PhantomLoginManager>();

	private readonly INavigation navigation;
	private readonly PhantomLoginStore loginStore;
	private readonly UserManager<IdentityUser> userManager;
	private readonly SignInManager<IdentityUser> signInManager;

	public PhantomLoginManager(INavigation navigation, PhantomLoginStore loginStore, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager) {
		this.navigation = navigation;
		this.loginStore = loginStore;
		this.userManager = userManager;
		this.signInManager = signInManager;
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
		await signInManager.SignOutAsync();
	}

	internal async Task<string?> ProcessTokenAndGetReturnUrl(string token) {
		var entry = loginStore.Pop(token);
		if (entry == null) {
			return null;
		}
		
		var result = await signInManager.PasswordSignInAsync(entry.User, entry.Password, lockoutOnFailure: false, isPersistent: true);
		if (result == SignInResult.Success) {
			Logger.Information("Successful login for {Username}.", entry.User.UserName);
			return entry.ReturnUrl;
		}
		else {
			Logger.Warning("Error logging in {Username}: {Result}.", entry.User.UserName, result);
			return null;
		}
	}
}
