using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record GetRolesMessage : IMessageToController<ImmutableArray<RoleInfo>> {
	public Task<ImmutableArray<RoleInfo>> Accept(IMessageToControllerListener listener) {
		return listener.HandleGetRoles(this);
	}
}
