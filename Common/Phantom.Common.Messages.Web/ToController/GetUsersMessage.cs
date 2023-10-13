using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record GetUsersMessage : IMessageToController<ImmutableArray<UserInfo>> {
	public Task<ImmutableArray<UserInfo>> Accept(IMessageToControllerListener listener) {
		return listener.HandleGetUsers(this);
	}
}
