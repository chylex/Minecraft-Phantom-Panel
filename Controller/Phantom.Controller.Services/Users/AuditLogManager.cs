using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.AuditLog;
using Phantom.Common.Data.Web.Users;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Repositories;
using Phantom.Controller.Services.Users.Sessions;

namespace Phantom.Controller.Services.Users;

sealed class AuditLogManager {
	private readonly IDbContextProvider dbProvider;
	
	public AuditLogManager(IDbContextProvider dbProvider) {
		this.dbProvider = dbProvider;
	}
	
	public async Task<Result<ImmutableArray<AuditLogItem>, UserActionFailure>> GetMostRecentItems(LoggedInUser loggedInUser, int count) {
		if (!loggedInUser.CheckPermission(Permission.ViewAudit)) {
			return UserActionFailure.NotAuthorized;
		}
		
		await using var db = dbProvider.Lazy();
		return await new AuditLogRepository(db).GetMostRecentItems(count, CancellationToken.None);
	}
}
