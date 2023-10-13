namespace Phantom.Controller.Database;

public interface IDbContextProvider {
	ApplicationDbContext Eager();
	ILazyDbContext Lazy();
}
