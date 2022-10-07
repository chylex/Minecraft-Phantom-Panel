using Microsoft.EntityFrameworkCore;
using Phantom.Server.Database.Entities;

namespace Phantom.Server.Database.Factories; 

public sealed class InstanceEntityUpsert : AbstractUpsertHelper<InstanceEntity> {
	internal InstanceEntityUpsert(ApplicationDbContext ctx) : base(ctx) {}

	private protected override DbSet<InstanceEntity> Set => Ctx.Instances;

	private protected override InstanceEntity Construct(Guid guid) {
		return new InstanceEntity(guid);
	}
}
