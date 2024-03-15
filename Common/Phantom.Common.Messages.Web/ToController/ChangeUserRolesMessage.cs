using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Web.Users;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ChangeUserRolesMessage(
	[property: MemoryPackOrder(0)] Guid LoggedInUserGuid,
	[property: MemoryPackOrder(1)] Guid SubjectUserGuid,
	[property: MemoryPackOrder(2)] ImmutableHashSet<Guid> AddToRoleGuids,
	[property: MemoryPackOrder(3)] ImmutableHashSet<Guid> RemoveFromRoleGuids
) : IMessageToController, ICanReply<ChangeUserRolesResult>;
