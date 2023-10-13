using System.Collections.Immutable;
using MemoryPack;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record GetUserRolesMessage(
	[property: MemoryPackOrder(0)] ImmutableHashSet<Guid> UserGuids
) : IMessageToController<ImmutableDictionary<Guid, ImmutableArray<Guid>>> {
	public Task<ImmutableDictionary<Guid, ImmutableArray<Guid>>> Accept(IMessageToControllerListener listener) {
		return listener.HandleGetUserRoles(this);
	}
}
