using MemoryPack;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record LogInMessage(
	[property: MemoryPackOrder(0)] string Username,
	[property: MemoryPackOrder(1)] string Password
) : IMessageToController<LogInSuccess?> {
	public Task<LogInSuccess?> Accept(IMessageToControllerListener listener) {
		return listener.HandleLogIn(this);
	}
}
