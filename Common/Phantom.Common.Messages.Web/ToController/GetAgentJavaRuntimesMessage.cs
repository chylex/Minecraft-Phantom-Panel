using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Java;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record GetAgentJavaRuntimesMessage : IMessageToController<ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>>> {
	public Task<ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>>> Accept(IMessageToControllerListener listener) {
		return listener.HandleGetAgentJavaRuntimes(this);
	}
}
