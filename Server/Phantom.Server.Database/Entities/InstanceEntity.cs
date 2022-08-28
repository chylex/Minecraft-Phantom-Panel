using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Phantom.Common.Data;

namespace Phantom.Server.Database.Entities; 

[Table("Instances", Schema = "agents")]
public class InstanceEntity {
	[Key]
	public Guid InstanceGuid { get; set; }

	public Guid AgentGuid { get; set; }

	public string InstanceName { get; set; }
	public ushort ServerPort { get; set; }
	public ushort RconPort { get; set; }
	public string MinecraftVersion { get; set; }
	public MinecraftServerKind MinecraftServerKind { get; set; }
	public RamAllocationUnits MemoryAllocation { get; set; }

	public InstanceEntity() {
		InstanceName = null!;
		MinecraftVersion = null!;
	}

	public InstanceInfo AsInstanceInfo => new (AgentGuid, InstanceGuid, InstanceName, ServerPort, RconPort, MinecraftVersion, MinecraftServerKind, MemoryAllocation);
	
	public void SetFromInstanceInfo(InstanceInfo info) {
		InstanceGuid = info.InstanceGuid;
		AgentGuid = info.AgentGuid;
		InstanceName = info.InstanceName;
		ServerPort = info.ServerPort;
		RconPort = info.RconPort;
		MinecraftVersion = info.MinecraftVersion;
		MinecraftServerKind = info.MinecraftServerKind;
		MemoryAllocation = info.MemoryAllocation;
	}
}
