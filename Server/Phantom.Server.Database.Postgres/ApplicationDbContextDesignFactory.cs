using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Phantom.Server.Database.Postgres; 

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class ApplicationDbContextDesignFactory : IDesignTimeDbContextFactory<ApplicationDbContext> {
	public ApplicationDbContext CreateDbContext(string[] args) {
		var opts = new DbContextOptionsBuilder<ApplicationDbContext>();
		opts.UseNpgsql();
		return new ApplicationDbContext(opts.Options);
	}
}
