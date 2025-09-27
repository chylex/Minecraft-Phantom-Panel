using MemoryPack;

namespace Phantom.Common.Data.Web.Agent;

[MemoryPackable]
[MemoryPackUnion(tag: 0, typeof(AgentIsOffline))]
[MemoryPackUnion(tag: 1, typeof(AgentIsDisconnected))]
[MemoryPackUnion(tag: 2, typeof(AgentIsOnline))]
public partial interface IAgentConnectionStatus;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AgentIsOffline : IAgentConnectionStatus;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AgentIsDisconnected([property: MemoryPackOrder(0)] DateTimeOffset LastPingTime) : IAgentConnectionStatus;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record AgentIsOnline : IAgentConnectionStatus;

public static class AgentConnectionStatus {
	public static readonly IAgentConnectionStatus Offline = new AgentIsOffline();
	public static readonly IAgentConnectionStatus Online = new AgentIsOnline();
	
	public static IAgentConnectionStatus Disconnected(DateTimeOffset lastPingTime) {
		return new AgentIsDisconnected(lastPingTime);
	}
}
