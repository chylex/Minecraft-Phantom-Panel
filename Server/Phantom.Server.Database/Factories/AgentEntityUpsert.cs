using Microsoft.EntityFrameworkCore;
using Phantom.Server.Database.Entities;

namespace Phantom.Server.Database.Factories; 

public sealed class AgentEntityUpsert : AbstractUpsertHelper<AgentEntity> {
	internal AgentEntityUpsert(ApplicationDbContext ctx) : base(ctx) {}

	private protected override DbSet<AgentEntity> Set => Ctx.Agents;
	
	private protected override AgentEntity Construct(Guid guid) {
		return new AgentEntity(guid);
	}
}
