using System.Security.Claims;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Web.Services.Authentication;

sealed class CustomClaimsPrincipal : ClaimsPrincipal {
	internal AuthenticatedUserInfo UserInfo { get; }

	internal CustomClaimsPrincipal(AuthenticatedUserInfo userInfo) : base(GetIdentity(userInfo)) {
		UserInfo = userInfo;
	}

	private static ClaimsIdentity GetIdentity(AuthenticatedUserInfo userInfo) {
		var identity = new ClaimsIdentity("Phantom");
		identity.AddClaim(new Claim(ClaimTypes.Name, userInfo.Name));
		return identity;
	}
}
