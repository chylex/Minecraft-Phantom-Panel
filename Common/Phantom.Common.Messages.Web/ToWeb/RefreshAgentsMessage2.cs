using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Web.Agent;

namespace Phantom.Common.Messages.Web.ToWeb;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record RefreshAgentsMessage2(
	[property: MemoryPackOrder(0)] ImmutableArray<RefreshAgentsMessage2.IItemAction> Actions
) : IMessageToWeb {
	[MemoryPackable]
	[MemoryPackUnion(tag: 0, typeof(RemoveItem))]
	[MemoryPackUnion(tag: 1, typeof(SetItem))]
	[MemoryPackUnion(tag: 2, typeof(UpdateItem))]
	public partial interface IItemAction;
	
	[MemoryPackable]
	public sealed partial record RemoveItem(Guid AgentGuid) : IItemAction;
	
	[MemoryPackable]
	public sealed partial record SetItem(Agent Agent) : IItemAction;
	
	[MemoryPackable]
	public sealed partial record UpdateItem(Guid AgentGuid, Agent.Update Update) : IItemAction;
}
