using System.Collections.Immutable;
using MemoryPack;

namespace Phantom.Common.Messages.Web.ToWeb;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceOutputMessage(
	[property: MemoryPackOrder(0)] Guid InstanceGuid,
	[property: MemoryPackOrder(1)] ImmutableArray<string> Lines
) : IMessageToWeb;
