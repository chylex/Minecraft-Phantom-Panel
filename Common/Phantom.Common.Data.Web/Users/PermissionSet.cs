using System.Collections.Immutable;
using MemoryPack;

namespace Phantom.Common.Data.Web.Users;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial class PermissionSet {
	public static PermissionSet None { get; } = new (ImmutableHashSet<string>.Empty);
	
	[MemoryPackOrder(0)]
	[MemoryPackInclude]
	private readonly ImmutableHashSet<string> permissionIds;
	
	public PermissionSet(ImmutableHashSet<string> permissionIds) {
		this.permissionIds = permissionIds;
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
