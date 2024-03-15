using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Web.Agent;

namespace Phantom.Common.Messages.Web.ToWeb; 

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record RefreshAgentsMessage(
	[property: MemoryPackOrder(0)] ImmutableArray<Agent> Agents
) : IMessageToWeb;
