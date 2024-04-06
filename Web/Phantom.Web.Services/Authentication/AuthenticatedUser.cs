using System.Collections.Immutable;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Web.Services.Authentication;

public sealed record AuthenticatedUser(AuthenticatedUserInfo Info, ImmutableArray<byte> Token) {
	public bool CheckPermission(Permission permission) {
		return Info.Permissions.Check(permission);
	}
}
