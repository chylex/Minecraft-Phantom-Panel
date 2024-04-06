using System.Collections.Immutable;
using MemoryPack;

namespace Phantom.Common.Data.Web.Users;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record LogInSuccess(
	[property: MemoryPackOrder(0)] AuthenticatedUserInfo UserInfo,
	[property: MemoryPackOrder(1)] ImmutableArray<byte> AuthToken
);
