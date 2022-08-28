using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Phantom.Server.Database.Entities;

[Table("Agents", Schema = "agents")]
public sealed class AgentEntity {
	[Key]
	public Guid AgentId { get; set; } // TODO rename
	
	public string Name { get; set; }

	public AgentEntity() {
		Name = null!;
	}
}
