using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Web.AuditLog;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record GetAuditLogMessage(
	[property: MemoryPackOrder(0)] int Count
) : IMessageToController<ImmutableArray<AuditLogItem>> {
	public Task<ImmutableArray<AuditLogItem>> Accept(IMessageToControllerListener listener) {
		return listener.HandleGetAuditLog(this);
	}
}
