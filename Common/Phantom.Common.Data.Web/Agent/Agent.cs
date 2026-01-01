using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Agent;

namespace Phantom.Common.Data.Web.Agent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record Agent(
	[property: MemoryPackOrder(0)] Guid AgentGuid,
	[property: MemoryPackOrder(1)] AgentConfiguration Configuration,
	[property: MemoryPackOrder(2)] ImmutableArray<byte> ConnectionKey,
	[property: MemoryPackOrder(3)] AgentRuntimeInfo RuntimeInfo,
	[property: MemoryPackOrder(4)] AgentStats? Stats,
	[property: MemoryPackOrder(5)] IAgentConnectionStatus ConnectionStatus
) {
	[MemoryPackIgnore]
	public RamAllocationUnits? AvailableMemory => RuntimeInfo.MaxMemory - Stats?.RunningInstanceMemory;
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record Update(
		[property: MemoryPackOrder(0)] Optional<AgentConfiguration> Configuration,
		[property: MemoryPackOrder(1)] Optional<ImmutableArray<byte>> ConnectionKey,
		[property: MemoryPackOrder(2)] Optional<AgentRuntimeInfo> RuntimeInfo,
		[property: MemoryPackOrder(3)] OptionalNullable<AgentStats> Stats,
		[property: MemoryPackOrder(4)] Optional<IAgentConnectionStatus> ConnectionStatus
	) {
		public Update Merge(Update newer) => new (
			newer.Configuration.Or(Configuration),
			newer.ConnectionKey.Or(ConnectionKey),
			newer.RuntimeInfo.Or(RuntimeInfo),
			newer.Stats.Or(Stats),
			newer.ConnectionStatus.Or(ConnectionStatus)
		);
		
		public Agent Apply(Agent target) => new (
			target.AgentGuid,
			Configuration.Or(target.Configuration),
			ConnectionKey.Or(target.ConnectionKey),
			RuntimeInfo.Or(target.RuntimeInfo),
			Stats.Or(target.Stats),
			ConnectionStatus.Or(target.ConnectionStatus)
		);
	}
}
