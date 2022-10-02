using MessagePack;
using Phantom.Common.Data.Instance;

namespace Phantom.Common.Messages.ToServer;

[MessagePackObject]
public sealed record ReportInstanceStatusMessage(
	[property: Key(0)] Guid InstanceGuid,
	[property: Key(1)] InstanceStatus InstanceStatus
) : IMessageToServer {
	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleReportInstanceStatus(this);
	}
}
