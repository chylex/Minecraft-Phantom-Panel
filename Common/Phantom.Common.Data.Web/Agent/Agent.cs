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
	
	public Agent With(Update update) => new (
		update.AgentGuid.Or(AgentGuid),
		update.Configuration.Or(Configuration),
		update.ConnectionKey.Or(ConnectionKey),
		update.RuntimeInfo.Or(RuntimeInfo),
		update.Stats.Or(Stats),
		update.ConnectionStatus.Or(ConnectionStatus)
	);
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record Update(
		[property: MemoryPackOrder(0)] Optional<Guid> AgentGuid,
		[property: MemoryPackOrder(1)] Optional<AgentConfiguration> Configuration,
		[property: MemoryPackOrder(2)] Optional<ImmutableArray<byte>> ConnectionKey,
		[property: MemoryPackOrder(3)] Optional<AgentRuntimeInfo> RuntimeInfo,
		[property: MemoryPackOrder(4)] OptionalNullable<AgentStats> Stats,
		[property: MemoryPackOrder(5)] Optional<IAgentConnectionStatus> ConnectionStatus
	) {
		public Update Merge(Update newer) => new (
			newer.AgentGuid.Or(AgentGuid),
			newer.Configuration.Or(Configuration),
			newer.ConnectionKey.Or(ConnectionKey),
			newer.RuntimeInfo.Or(RuntimeInfo),
			newer.Stats.Or(Stats),
			newer.ConnectionStatus.Or(ConnectionStatus)
		);
	}
}
