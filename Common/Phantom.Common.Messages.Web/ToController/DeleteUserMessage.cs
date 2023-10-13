using MemoryPack;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record DeleteUserMessage(
	[property: MemoryPackOrder(0)] Guid LoggedInUserGuid,
	[property: MemoryPackOrder(1)] Guid SubjectUserGuid
) : IMessageToController<DeleteUserResult> {
	public Task<DeleteUserResult> Accept(IMessageToControllerListener listener) {
		return listener.HandleDeleteUser(this);
	}
}
