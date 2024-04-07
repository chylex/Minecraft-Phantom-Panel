using System.ComponentModel.DataAnnotations.Schema;

namespace Phantom.Controller.Database.Entities;

[Table("UserAgentAccess", Schema = "identity")]
public sealed class UserAgentAccessEntity {
	public Guid UserGuid { get; init; }
	public Guid AgentGuid { get; init; }
	
	public UserAgentAccessEntity(Guid userGuid, Guid agentGuid) {
		UserGuid = userGuid;
		AgentGuid = agentGuid;
	}
}
