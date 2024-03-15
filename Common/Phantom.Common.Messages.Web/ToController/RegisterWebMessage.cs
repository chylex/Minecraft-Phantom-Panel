using MemoryPack;
using Phantom.Common.Data;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record RegisterWebMessage(
	[property: MemoryPackOrder(0)] AuthToken AuthToken
) : IMessageToController;
