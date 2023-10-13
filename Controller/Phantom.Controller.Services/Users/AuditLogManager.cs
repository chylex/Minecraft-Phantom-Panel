using System.Collections.Immutable;
using Phantom.Common.Data.Web.AuditLog;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Repositories;

namespace Phantom.Controller.Services.Users; 

sealed class AuditLogManager {
	private readonly IDbContextProvider dbProvider;

	public AuditLogManager(IDbContextProvider dbProvider) {
		this.dbProvider = dbProvider;
	}

	public async Task<ImmutableArray<AuditLogItem>> GetMostRecentItems(int count) {
		await using var db = dbProvider.Lazy();
		return await new AuditLogRepository(db).GetMostRecentItems(count, CancellationToken.None);
	}
}
