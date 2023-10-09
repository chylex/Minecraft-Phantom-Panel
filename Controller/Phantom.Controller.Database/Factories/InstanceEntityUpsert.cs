using Microsoft.EntityFrameworkCore;
using Phantom.Controller.Database.Entities;

namespace Phantom.Controller.Database.Factories; 

public sealed class InstanceEntityUpsert : AbstractUpsertHelper<InstanceEntity> {
	internal InstanceEntityUpsert(ApplicationDbContext ctx) : base(ctx) {}

	private protected override DbSet<InstanceEntity> Set => Ctx.Instances;

	private protected override InstanceEntity Construct(Guid guid) {
		return new InstanceEntity(guid);
	}
}
