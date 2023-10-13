using MemoryPack;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record CreateOrUpdateAdministratorUserMessage(
	[property: MemoryPackOrder(0)] string Username,
	[property: MemoryPackOrder(1)] string Password
) : IMessageToController<CreateOrUpdateAdministratorUserResult> {
	public Task<CreateOrUpdateAdministratorUserResult> Accept(IMessageToControllerListener listener) {
		return listener.HandleCreateOrUpdateAdministratorUser(this);
	}
}
