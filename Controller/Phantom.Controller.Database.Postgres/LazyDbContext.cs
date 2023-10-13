namespace Phantom.Controller.Database.Postgres;

sealed class LazyDbContext : ILazyDbContext {
	public ApplicationDbContext Ctx => cachedContext ??= contextFactory.Eager();

	private readonly ApplicationDbContextFactory contextFactory;
	private ApplicationDbContext? cachedContext;

	internal LazyDbContext(ApplicationDbContextFactory contextFactory) {
		this.contextFactory = contextFactory;
	}

	public ValueTask DisposeAsync() {
		return cachedContext?.DisposeAsync() ?? ValueTask.CompletedTask;
	}
}
