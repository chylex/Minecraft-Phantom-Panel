using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Minecraft;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record GetMinecraftVersionsMessage : IMessageToController<ImmutableArray<MinecraftVersion>> {
	public Task<ImmutableArray<MinecraftVersion>> Accept(IMessageToControllerListener listener) {
		return listener.HandleGetMinecraftVersions(this);
	}
}
