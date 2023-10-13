using MemoryPack;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Web.ToWeb; 

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record RegisterWebResultMessage(
	[property: MemoryPackOrder(0)] bool Success
) : IMessageToWeb {
	public Task<NoReply> Accept(IMessageToWebListener listener) {
		return listener.HandleRegisterWebResult(this);
	}
}
