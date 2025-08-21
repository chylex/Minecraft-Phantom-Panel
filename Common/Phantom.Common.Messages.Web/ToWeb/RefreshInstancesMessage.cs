using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Web.Instance;

namespace Phantom.Common.Messages.Web.ToWeb;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record RefreshInstancesMessage(
	[property: MemoryPackOrder(0)] ImmutableArray<Instance> Instances
) : IMessageToWeb;
