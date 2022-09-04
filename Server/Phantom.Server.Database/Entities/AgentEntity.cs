using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Phantom.Server.Database.Entities;

[Table("Agents", Schema = "agents")]
public sealed class AgentEntity {
	[Key]
	public Guid AgentGuid { get; set; }
	
	public string Name { get; set; }

	public AgentEntity() {
		Name = null!;
	}
}
