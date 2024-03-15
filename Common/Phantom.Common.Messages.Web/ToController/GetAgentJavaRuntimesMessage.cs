using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Java;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record GetAgentJavaRuntimesMessage : IMessageToController, ICanReply<ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>>>;
