using MemoryPack;

namespace Phantom.Common.Data.Web.Users;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record UserInfo(
	[property: MemoryPackOrder(0)] Guid Guid,
	[property: MemoryPackOrder(1)] string Name
);
