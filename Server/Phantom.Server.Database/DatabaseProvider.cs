using Microsoft.Extensions.DependencyInjection;

namespace Phantom.Server.Database; 

public sealed class DatabaseProvider {
	private readonly IServiceScopeFactory serviceScopeFactory;
	
	public DatabaseProvider(IServiceScopeFactory serviceScopeFactory) {
		this.serviceScopeFactory = serviceScopeFactory;
	}

	public Scope CreateScope() {
		return new Scope(serviceScopeFactory.CreateScope());
	}
	
	public readonly struct Scope : IDisposable {
		private readonly IServiceScope scope;

		public ApplicationDbContext Db { get; }

		public Scope(IServiceScope scope) {
			this.scope = scope;
			this.Db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		}

		public void Dispose() {
			scope.Dispose();
		}
	}
}
