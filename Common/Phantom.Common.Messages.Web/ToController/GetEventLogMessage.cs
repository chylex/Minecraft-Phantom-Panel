using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Web.EventLog;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record GetEventLogMessage(
	[property: MemoryPackOrder(0)] int Count
) : IMessageToController<ImmutableArray<EventLogItem>> {
	public Task<ImmutableArray<EventLogItem>> Accept(IMessageToControllerListener listener) {
		return listener.HandleGetEventLog(this);
	}
}
