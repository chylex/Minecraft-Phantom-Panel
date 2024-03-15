using MemoryPack;

namespace Phantom.Common.Messages.Web.ToWeb; 

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record RegisterWebResultMessage(
	[property: MemoryPackOrder(0)] bool Success
) : IMessageToWeb;
