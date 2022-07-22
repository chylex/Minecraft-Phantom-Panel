using Microsoft.EntityFrameworkCore;
using Npgsql;
using Phantom.Server.Web;

static string RequireEnv(string variableName) {
	return Environment.GetEnvironmentVariable(variableName) ?? throw new Exception("Missing environment variable: " + variableName);
}

var connectionStringBuilder = new NpgsqlConnectionStringBuilder();
try {
	connectionStringBuilder.Host = RequireEnv("PG_HOST");
	connectionStringBuilder.Port = int.Parse(RequireEnv("PG_PORT"));
	connectionStringBuilder.Username = RequireEnv("PG_USER");
	connectionStringBuilder.Password = RequireEnv("PG_PASS");
	connectionStringBuilder.Database = RequireEnv("PG_DATABASE");
} catch (Exception e) {
	Console.WriteLine(e.Message);
	Environment.Exit(1);
}

Console.WriteLine("Launching Phantom Panel...");
Launcher.Launch(options => options.UseNpgsql(connectionStringBuilder.ToString()));
