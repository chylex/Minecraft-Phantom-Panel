using MemoryPack;

namespace Phantom.Common.Data.Web.Users;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AuthenticatedUserInfo(
	[property: MemoryPackOrder(0)] Guid Guid,
	[property: MemoryPackOrder(1)] string Name,
	[property: MemoryPackOrder(2)] PermissionSet Permissions
);
