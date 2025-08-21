using Microsoft.EntityFrameworkCore;

namespace Phantom.Controller.Database.Factories;

public abstract class AbstractUpsertHelper<T> where T : class {
	private protected readonly ApplicationDbContext Ctx;
	
	internal AbstractUpsertHelper(ApplicationDbContext ctx) {
		this.Ctx = ctx;
	}
	
	private protected abstract DbSet<T> Set { get; }
	
	private protected abstract T Construct(Guid guid);
	
	public T Fetch(Guid guid) {
		DbSet<T> set = Set;
		T? entity = set.Find(guid);
		
		if (entity == null) {
			entity = Construct(guid);
			set.Add(entity);
		}
		else {
			set.Update(entity);
		}
		
		return entity;
	}
}
