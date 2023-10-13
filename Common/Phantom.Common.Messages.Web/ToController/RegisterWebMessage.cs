using MemoryPack;
using Phantom.Common.Data;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record RegisterWebMessage(
	[property: MemoryPackOrder(0)] AuthToken AuthToken
) : IMessageToController {
	public Task<NoReply> Accept(IMessageToControllerListener listener) {
		return listener.HandleRegisterWeb(this);
	}
}
