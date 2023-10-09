using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Phantom.Common.Data;
using Phantom.Common.Data.Minecraft;

namespace Phantom.Controller.Database.Entities; 

[Table("Instances", Schema = "agents")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public sealed class InstanceEntity {
	[Key]
	public Guid InstanceGuid { get; set; }

	public Guid AgentGuid { get; set; }

	public string InstanceName { get; set; }
	public ushort ServerPort { get; set; }
	public ushort RconPort { get; set; }
	public string MinecraftVersion { get; set; }
	public MinecraftServerKind MinecraftServerKind { get; set; }
	public RamAllocationUnits MemoryAllocation { get; set; }
	public Guid JavaRuntimeGuid { get; set; }
	public string JvmArguments { get; set; }
	public bool LaunchAutomatically { get; set; }

	internal InstanceEntity(Guid instanceGuid) {
		InstanceGuid = instanceGuid;
		InstanceName = null!;
		MinecraftVersion = null!;
		JvmArguments = string.Empty;
	}
}
