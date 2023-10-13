namespace Phantom.Controller.Database;

public interface ILazyDbContext : IAsyncDisposable {
	ApplicationDbContext Ctx { get; }
}
