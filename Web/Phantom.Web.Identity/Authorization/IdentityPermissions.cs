using System.Collections.Immutable;
using Phantom.Web.Identity.Data;

namespace Phantom.Web.Identity.Authorization; 

public sealed class IdentityPermissions {
	internal static IdentityPermissions None { get; } = new ();
	
	private readonly ImmutableHashSet<string> permissionIds;

	internal IdentityPermissions(IQueryable<string> permissionIdsQuery) {
		this.permissionIds = permissionIdsQuery.ToImmutableHashSet();
	}

	private IdentityPermissions() {
		this.permissionIds = ImmutableHashSet<string>.Empty;
	}

	public bool Check(Permission? permission) {
		while (permission != null) {
			if (!permissionIds.Contains(permission.Id)) {
				return false;
			}

			permission = permission.Parent;
		}

		return true;
	}
}
