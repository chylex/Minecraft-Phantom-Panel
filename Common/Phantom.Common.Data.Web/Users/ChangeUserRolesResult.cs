using System.Collections.Immutable;
using MemoryPack;

namespace Phantom.Common.Data.Web.Users; 

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ChangeUserRolesResult(
	[property: MemoryPackOrder(0)] ImmutableHashSet<Guid> AddedToRoleGuids,
	[property: MemoryPackOrder(1)] ImmutableHashSet<Guid> RemovedFromRoleGuids
);
