using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Minecraft;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record GetMinecraftVersionsMessage : IMessageToController, ICanReply<ImmutableArray<MinecraftVersion>>;
