using Microsoft.EntityFrameworkCore;
using Phantom.Common.Logging;
using Phantom.Utils.Tasks;
using Serilog;

namespace Phantom.Controller.Database;

public static class DatabaseMigrator {
	private static readonly ILogger Logger = PhantomLogger.Create(nameof(DatabaseMigrator));
	
	public static async Task Run(IDatabaseProvider databaseProvider, CancellationToken cancellationToken) {
		await using var ctx = databaseProvider.Provide();

		Logger.Information("Connecting to database...");

		var retryConnection = new Throttler(TimeSpan.FromSeconds(10));
		while (!await ctx.Database.CanConnectAsync(cancellationToken)) {
			Logger.Warning("Cannot connect to database, retrying...");
			await retryConnection.Wait();
		}

		Logger.Information("Running migrations...");
		await ctx.Database.MigrateAsync(CancellationToken.None);
	}
}
