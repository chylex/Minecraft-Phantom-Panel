using Phantom.Common.Data.Instance;

namespace Phantom.Server.Services.Agents;

public sealed record Agent(
	Guid Guid,
	string Name,
	ushort Version,
	ushort MaxInstances,
	RamAllocationUnits MaxMemory,
	DateTimeOffset? LastPing = null
) {
	internal AgentConnection? Connection { get; init; }

	public bool IsOnline => Connection is not null;
	public bool IsOffline => Connection is null;
	
	internal Agent AsOffline() => this with {
		Connection = null
	};
}
