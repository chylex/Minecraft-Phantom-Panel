using MemoryPack;
using Phantom.Common.Data.Instance;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.ToServer;

[MemoryPackable]
public sealed partial record ReportInstanceStatusMessage(
	[property: MemoryPackOrder(0)] Guid InstanceGuid,
	[property: MemoryPackOrder(1)] IInstanceStatus InstanceStatus
) : IMessageToServer {
	public Task<NoReply> Accept(IMessageToServerListener listener) {
		return listener.HandleReportInstanceStatus(this);
	}
}
