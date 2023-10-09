using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Phantom.Controller.Database.Postgres;

public sealed class ApplicationDbContextDesignFactory : IDesignTimeDbContextFactory<ApplicationDbContext> {
	public ApplicationDbContext CreateDbContext(string[] args) {
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
		              .UseNpgsql(static options => options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName))
		              .Options;

		return new ApplicationDbContext(options);
	}
}
