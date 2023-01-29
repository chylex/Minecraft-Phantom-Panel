using MemoryPack;
using Phantom.Common.Data;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.ToServer; 

[MemoryPackable]
public partial record ReportAgentStatusMessage(
	[property: MemoryPackOrder(0)] int RunningInstanceCount,
	[property: MemoryPackOrder(1)] RamAllocationUnits RunningInstanceMemory
) : IMessageToServer {
	public Task<NoReply> Accept(IMessageToServerListener listener) {
		return listener.HandleReportAgentStatus(this);
	}
}
