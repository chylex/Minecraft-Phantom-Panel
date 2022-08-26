using Microsoft.EntityFrameworkCore;

namespace Phantom.Server.Database;

public static class DatabaseExtensions {
	public static void Upsert<T>(this DbSet<T> set, Guid id, Action<Guid, T> update) where T : class, new() {
		var existing = set.Find(id);
		if (existing == null) {
			var entity = new T();
			update(id, entity);
			set.Add(entity);
		}
		else {
			update(id, existing);
			set.Update(existing);
		}
	}
}
