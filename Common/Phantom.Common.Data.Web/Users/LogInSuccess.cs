using System.Collections.Immutable;
using MemoryPack;

namespace Phantom.Common.Data.Web.Users;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record LogInSuccess (
	[property: MemoryPackOrder(0)] Guid UserGuid,
	[property: MemoryPackOrder(1)] PermissionSet Permissions,
	[property: MemoryPackOrder(2)] ImmutableArray<byte> Token
);
