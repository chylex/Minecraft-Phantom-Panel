using MemoryPack;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record CreateUserMessage(
	[property: MemoryPackOrder(0)] Guid LoggedInUserGuid,
	[property: MemoryPackOrder(1)] string Username,
	[property: MemoryPackOrder(2)] string Password
) : IMessageToController<CreateUserResult> {
	public Task<CreateUserResult> Accept(IMessageToControllerListener listener) {
		return listener.HandleCreateUser(this);
	}
}
