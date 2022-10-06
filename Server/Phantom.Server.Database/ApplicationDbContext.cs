using Microsoft.EntityFrameworkCore;

namespace Phantom.Server.Database;

public class ApplicationDbContext : DbContext {
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}
}
