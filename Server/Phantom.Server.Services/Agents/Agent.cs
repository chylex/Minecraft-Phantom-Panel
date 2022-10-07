using Phantom.Common.Data;
using Phantom.Common.Data.Agent;

namespace Phantom.Server.Services.Agents;

public sealed record Agent(
	Guid Guid,
	string Name,
	ushort Version,
	ushort MaxInstances,
	RamAllocationUnits MaxMemory,
	AllowedPorts? AllowedServerPorts = null,
	AllowedPorts? AllowedRconPorts = null,
	DateTimeOffset? LastPing = null
) {
	internal AgentConnection? Connection { get; init; }
	
	internal Agent(AgentInfo info) : this(info.Guid, info.Name, info.Version, info.MaxInstances, info.MaxMemory, info.AllowedServerPorts, info.AllowedRconPorts) {}

	public bool IsOnline => Connection is not null;
	public bool IsOffline => Connection is null;
	
	internal Agent AsOffline() => this with {
		Connection = null
	};
}
