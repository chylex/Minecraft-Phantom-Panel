using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Web.EventLog;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record GetEventLogMessage(
	[property: MemoryPackOrder(0)] int Count
) : IMessageToController, ICanReply<ImmutableArray<EventLogItem>>;
