using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Java;

namespace Phantom.Common.Messages.Agent.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AdvertiseJavaRuntimesMessage(
	[property: MemoryPackOrder(0)] ImmutableArray<TaggedJavaRuntime> Runtimes
) : IMessageToController;
