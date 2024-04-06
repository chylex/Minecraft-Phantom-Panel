using System.Security.Claims;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Web.Services.Authentication;

sealed class CustomClaimsPrincipal : ClaimsPrincipal {
	internal AuthenticatedUser User { get; }

	internal CustomClaimsPrincipal(AuthenticatedUser user) : base(GetIdentity(user.Info)) {
		User = user;
	}

	private static ClaimsIdentity GetIdentity(AuthenticatedUserInfo userInfo) {
		var identity = new ClaimsIdentity("Phantom");
		identity.AddClaim(new Claim(ClaimTypes.Name, userInfo.Name));
		return identity;
	}
}
