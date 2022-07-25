using Microsoft.EntityFrameworkCore;
using Npgsql;
using Phantom.Utils.Logging;
using GrpcLauncher = Phantom.Server.Grpc.Launcher;
using WebLauncher = Phantom.Server.Web.Launcher;

static string RequireEnv(string variableName) {
	return Environment.GetEnvironmentVariable(variableName) ?? throw new Exception("Missing environment variable: " + variableName);
}

try {
	var connectionStringBuilder = new NpgsqlConnectionStringBuilder();
	try {
		connectionStringBuilder.Host = RequireEnv("PG_HOST");
		connectionStringBuilder.Port = int.Parse(RequireEnv("PG_PORT"));
		connectionStringBuilder.Username = RequireEnv("PG_USER");
		connectionStringBuilder.Password = RequireEnv("PG_PASS");
		connectionStringBuilder.Database = RequireEnv("PG_DATABASE");
	} catch (Exception e) {
		PhantomLogger.Root.Fatal(e.Message);
		Environment.Exit(1);
	}

	PhantomLogger.Root.InformationHeading("Launching Phantom Panel server...");

	Task.WaitAll(
		GrpcLauncher.Launch(PhantomLogger.Create("Grpc"), port: 9401),
		WebLauncher.Launch(PhantomLogger.Create("Web"), port: 9400, options => options.UseNpgsql(connectionStringBuilder.ToString()))
	);
} finally {
	PhantomLogger.Dispose();
}
