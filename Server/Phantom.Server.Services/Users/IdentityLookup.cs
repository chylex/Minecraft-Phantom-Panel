using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Phantom.Server.Services.Users;

public sealed class IdentityLookup {
	private readonly UserManager<IdentityUser> userManager;

	public IdentityLookup(UserManager<IdentityUser> userManager) {
		this.userManager = userManager;
	}

	public string? GetAuthenticatedUserId(ClaimsPrincipal user) {
		return user.Identity is { IsAuthenticated: true } ? userManager.GetUserId(user) : null;
	}
}
