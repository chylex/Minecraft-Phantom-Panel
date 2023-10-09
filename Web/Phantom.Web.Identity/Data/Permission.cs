namespace Phantom.Web.Identity.Data;

public sealed record Permission(string Id, Permission? Parent) {
	private static readonly List<Permission> AllPermissions = new ();
	public static IEnumerable<Permission> All => AllPermissions;
	
	private static Permission Register(string id, Permission? parent = null) {
		var permission = new Permission(id, parent);
		AllPermissions.Add(permission);
		return permission;
	}

	private Permission RegisterChild(string id) {
		return Register(id, this);
	}

	public const string ViewInstancesPolicy = "Instances.View";
	public static readonly Permission ViewInstances = Register(ViewInstancesPolicy);
	
	public const string ViewInstanceLogsPolicy = "Instances.Logs.View";
	public static readonly Permission ViewInstanceLogs = ViewInstances.RegisterChild(ViewInstanceLogsPolicy);
	
	public const string CreateInstancesPolicy = "Instances.Create";
	public static readonly Permission CreateInstances = ViewInstances.RegisterChild(CreateInstancesPolicy);
	
	public const string ControlInstancesPolicy = "Instances.Control";
	public static readonly Permission ControlInstances = ViewInstances.RegisterChild(ControlInstancesPolicy);
	
	public const string ViewUsersPolicy = "Users.View";
	public static readonly Permission ViewUsers = Register(ViewUsersPolicy);
	
	public const string EditUsersPolicy = "Users.Edit";
	public static readonly Permission EditUsers = ViewUsers.RegisterChild(EditUsersPolicy);
	
	public const string ViewAuditPolicy = "Audit.View";
	public static readonly Permission ViewAudit = Register(ViewAuditPolicy);
	
	public const string ViewEventsPolicy = "Events.View";
	public static readonly Permission ViewEvents = Register(ViewEventsPolicy);
}
