namespace Phantom.Server.Web.Identity.Data;

public sealed record Permission(string Id, Permission? Parent) {
	private static readonly List<Permission> AllPermissions = new ();
	public static IEnumerable<Permission> All => AllPermissions;
	
	private static Permission Register(string id, Permission? parent = null) {
		var permission = new Permission(id, parent);
		AllPermissions.Add(permission);
		return permission;
	}
}
