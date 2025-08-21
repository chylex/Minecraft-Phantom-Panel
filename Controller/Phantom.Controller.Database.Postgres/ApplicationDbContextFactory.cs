using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Phantom.Controller.Database.Postgres;

public sealed class ApplicationDbContextFactory : IDbContextProvider {
	private readonly PooledDbContextFactory<ApplicationDbContext> factory;
	
	public ApplicationDbContextFactory(string connectionString) {
		this.factory = new PooledDbContextFactory<ApplicationDbContext>(CreateOptions(connectionString), poolSize: 32);
	}
	
	public ApplicationDbContext Eager() {
		return factory.CreateDbContext();
	}
	
	public ILazyDbContext Lazy() {
		return new LazyDbContext(this);
	}
	
	private static DbContextOptions<ApplicationDbContext> CreateOptions(string connectionString) {
		var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
		builder.UseNpgsql(connectionString, ConfigureOptions);
		return builder.Options;
	}
	
	private static void ConfigureOptions(NpgsqlDbContextOptionsBuilder options) {
		options.CommandTimeout(10);
		options.MigrationsAssembly(typeof(ApplicationDbContextDesignFactory).Assembly.FullName);
	}
}
