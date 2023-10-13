using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ChangeUserRolesMessage(
	[property: MemoryPackOrder(0)] Guid LoggedInUserGuid,
	[property: MemoryPackOrder(1)] Guid SubjectUserGuid,
	[property: MemoryPackOrder(2)] ImmutableHashSet<Guid> AddToRoleGuids,
	[property: MemoryPackOrder(3)] ImmutableHashSet<Guid> RemoveFromRoleGuids
) : IMessageToController<ChangeUserRolesResult> {
	public Task<ChangeUserRolesResult> Accept(IMessageToControllerListener listener) {
		return listener.HandleChangeUserRoles(this);
	}
}
