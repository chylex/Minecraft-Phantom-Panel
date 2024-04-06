using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.EventLog;
using Phantom.Common.Data.Web.Users;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record GetEventLogMessage(
	[property: MemoryPackOrder(0)] ImmutableArray<byte> AuthToken,
	[property: MemoryPackOrder(1)] int Count
) : IMessageToController, ICanReply<Result<ImmutableArray<EventLogItem>, UserActionFailure>>;
