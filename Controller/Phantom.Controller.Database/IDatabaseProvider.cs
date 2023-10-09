namespace Phantom.Controller.Database;

public interface IDatabaseProvider {
	ApplicationDbContext Provide();
}
