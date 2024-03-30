using System.Collections.Immutable;
using MemoryPack;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record LogOutMessage(
	[property: MemoryPackOrder(0)] Guid UserGuid,
	[property: MemoryPackOrder(1)] ImmutableArray<byte> SessionToken
) : IMessageToController;
