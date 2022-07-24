using Microsoft.EntityFrameworkCore;
using Npgsql;
using Phantom.Server.Web;
using Phantom.Utils.Logging;

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
		PhantomLogger.Base.Fatal(e.Message);
		Environment.Exit(1);
	}

	PhantomLogger.Base.InformationHeading("Launching Phantom Panel web server...");
	Launcher.Launch(PhantomLogger.Create("Web"), options => options.UseNpgsql(connectionStringBuilder.ToString()));
} finally {
	PhantomLogger.Base.Dispose();
}
