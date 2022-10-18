using Phantom.Common.Data;
using Phantom.Common.Data.Agent;

namespace Phantom.Server.Services.Agents;

public sealed record Agent(
	Guid Guid,
	string Name,
	ushort ProtocolVersion,
	string BuildVersion,
	ushort MaxInstances,
	RamAllocationUnits MaxMemory,
	AllowedPorts? AllowedServerPorts = null,
	AllowedPorts? AllowedRconPorts = null,
	DateTimeOffset? LastPing = null
) {
	internal AgentConnection? Connection { get; init; }
	
	public bool IsOnline { get; internal init; }
	public bool IsOffline => !IsOnline;

	internal Agent(AgentInfo info) : this(info.Guid, info.Name, info.ProtocolVersion, info.BuildVersion, info.MaxInstances, info.MaxMemory, info.AllowedServerPorts, info.AllowedRconPorts) {}

	internal Agent AsDisconnected() => this with {
		IsOnline = false
	};
	
	internal Agent AsOffline() => this with {
		Connection = null,
		IsOnline = false
	};
}
