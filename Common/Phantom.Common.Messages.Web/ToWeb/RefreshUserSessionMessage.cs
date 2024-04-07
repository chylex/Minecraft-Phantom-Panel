using MemoryPack;

namespace Phantom.Common.Messages.Web.ToWeb; 

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record RefreshUserSessionMessage(
	[property: MemoryPackOrder(0)] Guid UserGuid
) : IMessageToWeb;
