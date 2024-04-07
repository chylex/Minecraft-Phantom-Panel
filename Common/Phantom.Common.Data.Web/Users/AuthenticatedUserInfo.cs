using System.Collections.Immutable;
using MemoryPack;

namespace Phantom.Common.Data.Web.Users;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AuthenticatedUserInfo(
	[property: MemoryPackOrder(0)] Guid Guid,
	[property: MemoryPackOrder(1)] string Name,
	[property: MemoryPackOrder(2)] PermissionSet Permissions,
	[property: MemoryPackOrder(3)] ImmutableHashSet<Guid> ManagedAgentGuids
) {
	public bool CheckPermission(Permission permission) {
		return Permissions.Check(permission);
	}

	public bool HasAccessToAgent(Guid agentGuid) {
		return ManagedAgentGuids.Contains(agentGuid) || Permissions.Check(Permission.ManageAllAgents);
	}

	public ImmutableHashSet<Guid> FilterAccessibleAgentGuids(ImmutableHashSet<Guid> agentGuids) {
		return Permissions.Check(Permission.ManageAllAgents) ? agentGuids : agentGuids.Intersect(ManagedAgentGuids);
	}
}
