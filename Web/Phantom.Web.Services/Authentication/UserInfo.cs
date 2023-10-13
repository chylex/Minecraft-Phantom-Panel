using System.Security.Claims;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Web.Services.Authentication;

public sealed record UserInfo(Guid UserGuid, string Username, PermissionSet Permissions) {
	private const string AuthenticationType = "Phantom";

	internal ClaimsPrincipal AsClaimsPrincipal {
		get {
			var identity = new ClaimsIdentity(AuthenticationType);

			identity.AddClaim(new Claim(ClaimTypes.Name, Username));
			identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, UserGuid.ToString()));

			return new ClaimsPrincipal(identity);
		}
	}

	public static Guid? TryGetGuid(ClaimsPrincipal principal) {
		return principal.Identity is { IsAuthenticated: true, AuthenticationType: AuthenticationType } && principal.FindFirstValue(ClaimTypes.NameIdentifier) is {} guidStr && Guid.TryParse(guidStr, out var guid) ? guid : null;
	}
}
