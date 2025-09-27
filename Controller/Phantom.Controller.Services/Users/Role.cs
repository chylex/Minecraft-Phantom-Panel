using System.Collections.Immutable;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Controller.Services.Users;

public sealed record Role(Guid Guid, string Name, ImmutableArray<Permission> Permissions) {
	private static readonly List<Role> AllRoles = [];
	internal static IEnumerable<Role> All => AllRoles;
	
	private static Role Register(Guid guid, string name, ImmutableArray<Permission> permissions) {
		var role = new Role(guid, name, permissions);
		AllRoles.Add(role);
		return role;
	}
	
	private static Guid SystemRoleGuid(byte id) {
		return new Guid(a: 0, b: 0, c: 0, d: 0, e: 0, f: 0, g: 0, h: 0, i: 0, j: 0, id);
	}
	
	public static readonly Role Administrator = Register(SystemRoleGuid(1), "Administrator", [..Permission.All]);
	public static readonly Role InstanceManager = Register(SystemRoleGuid(2), "Instance Manager", [Permission.ViewInstances, Permission.ViewInstanceLogs, Permission.CreateInstances, Permission.ControlInstances, Permission.ViewEvents]);
}
