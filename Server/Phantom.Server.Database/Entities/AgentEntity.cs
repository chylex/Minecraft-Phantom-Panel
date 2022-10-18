using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Phantom.Common.Data;

namespace Phantom.Server.Database.Entities;

[Table("Agents", Schema = "agents")]
public sealed class AgentEntity {
	[Key]
	public Guid AgentGuid { get; set; }
	
	public string Name { get; set; }
	public ushort ProtocolVersion { get; set; }
	public string BuildVersion { get; set; }
	public ushort MaxInstances { get; set; }
	public RamAllocationUnits MaxMemory { get; set; }

	internal AgentEntity(Guid agentGuid) {
		AgentGuid = agentGuid;
		Name = null!;
		BuildVersion = null!;
	}
}
