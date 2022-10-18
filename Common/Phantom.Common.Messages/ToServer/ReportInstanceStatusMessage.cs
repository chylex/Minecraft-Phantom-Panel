using MemoryPack;
using Phantom.Common.Data.Instance;

namespace Phantom.Common.Messages.ToServer;

[MemoryPackable]
public sealed partial record ReportInstanceStatusMessage(
	[property: MemoryPackOrder(0)] Guid InstanceGuid,
	[property: MemoryPackOrder(1)] IInstanceStatus InstanceStatus
) : IMessageToServer {
	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleReportInstanceStatus(this);
	}
}
