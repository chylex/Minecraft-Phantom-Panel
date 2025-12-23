using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.Agent;
using Phantom.Utils.Rpc;

namespace Phantom.Controller.Database.Entities;

[Table("Agents", Schema = "agents")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public sealed class AgentEntity {
	[Key]
	public Guid AgentGuid { get; init; }
	
	public string Name { get; set; }
	public ushort? ProtocolVersion { get; set; }
	public string? BuildVersion { get; set; }
	public ushort? MaxInstances { get; set; }
	public RamAllocationUnits? MaxMemory { get; set; }
	
	[MaxLength(AuthSecret.Length)]
	public AuthSecret? AuthSecret { get; set; }
	
	public AgentConfiguration Configuration => new (Name);
	public AgentVersionInfo? VersionInfo => ProtocolVersion is {} protocolVersion && BuildVersion is {} buildVersion ? new AgentVersionInfo(protocolVersion, buildVersion) : null;
	public AgentRuntimeInfo RuntimeInfo => new (VersionInfo, MaxInstances, MaxMemory);
	
	internal AgentEntity(Guid agentGuid) {
		AgentGuid = agentGuid;
		Name = null!;
		BuildVersion = null!;
	}
}
