using System.Collections.Immutable;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Controller.Services.Users;

public sealed record Role(Guid Guid, string Name, ImmutableArray<Permission> Permissions) {
	private static readonly List<Role> AllRoles = new ();
	internal static IEnumerable<Role> All => AllRoles;
	
	private static Role Register(Guid guid, string name, ImmutableArray<Permission> permissions) {
		var role = new Role(guid, name, permissions);
		AllRoles.Add(role);
		return role;
	}

	private static Guid SystemRoleGuid(byte id) {
		return new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, id);
	}
	
	public static readonly Role Administrator = Register(SystemRoleGuid(1), "Administrator", Permission.All.ToImmutableArray());
	public static readonly Role InstanceManager = Register(SystemRoleGuid(2), "Instance Manager", ImmutableArray.Create(Permission.ViewInstances, Permission.ViewInstanceLogs, Permission.CreateInstances, Permission.ControlInstances, Permission.ViewEvents));
}
