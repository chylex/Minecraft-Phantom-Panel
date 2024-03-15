using System.Collections.Immutable;
using MemoryPack;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record GetUserRolesMessage(
	[property: MemoryPackOrder(0)] ImmutableHashSet<Guid> UserGuids
) : IMessageToController, ICanReply<ImmutableDictionary<Guid, ImmutableArray<Guid>>>;
