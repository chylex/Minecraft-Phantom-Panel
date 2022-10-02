using MessagePack;
using Phantom.Common.Data.Instance;

namespace Phantom.Common.Messages.ToServer;

[MessagePackObject]
public sealed record ReportInstanceStateMessage(
	[property: Key(0)] Guid InstanceGuid,
	[property: Key(1)] InstanceState InstanceState
) : IMessageToServer {
	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleReportInstanceState(this);
	}
}
