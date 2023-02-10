using System.Collections.Immutable;

namespace Phantom.Server.Web.Identity.Data;

public sealed record Role(string Name, ImmutableArray<Permission> Permissions) {
	private static readonly List<Role> AllRoles = new ();
	internal static IEnumerable<Role> All => AllRoles;
	
	private static Role Register(string name, ImmutableArray<Permission> permissions) {
		var role = new Role(name, permissions);
		AllRoles.Add(role);
		return role;
	}

	public static readonly Role Administrator = Register("Administrator", Permission.All.ToImmutableArray());
	public static readonly Role InstanceManager = Register("Instance Manager", ImmutableArray.Create(Permission.ViewInstances, Permission.ViewInstanceLogs, Permission.CreateInstances, Permission.ControlInstances, Permission.ViewEvents));
}
