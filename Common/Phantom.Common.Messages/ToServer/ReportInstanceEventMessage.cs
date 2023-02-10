using MemoryPack;
using Phantom.Common.Data.Instance;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.ToServer; 

[MemoryPackable]
public sealed partial record ReportInstanceEventMessage(
	[property: MemoryPackOrder(0)] Guid EventGuid,
	[property: MemoryPackOrder(1)] DateTime UtcTime,
	[property: MemoryPackOrder(2)] Guid InstanceGuid,
	[property: MemoryPackOrder(3)] IInstanceEvent Event
) : IMessageToServer {
	public Task<NoReply> Accept(IMessageToServerListener listener) {
		return listener.HandleReportInstanceEvent(this);
	}
}
