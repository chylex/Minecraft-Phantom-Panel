using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Web.AuditLog;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record GetAuditLogMessage(
	[property: MemoryPackOrder(0)] int Count
) : IMessageToController, ICanReply<ImmutableArray<AuditLogItem>>;
