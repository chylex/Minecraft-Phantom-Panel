using Microsoft.EntityFrameworkCore;
using Npgsql;
using Phantom.Utils.Logging;
using Phantom.Utils.Runtime;
using RpcConfiguration = Phantom.Server.Rpc.Configuration;
using RpcLauncher = Phantom.Server.Rpc.Launcher;
using WebConfiguration = Phantom.Server.Web.Configuration;
using WebLauncher = Phantom.Server.Web.Launcher;

static string RequireEnv(string variableName) {
	return Environment.GetEnvironmentVariable(variableName) ?? throw new Exception("Missing environment variable: " + variableName);
}

var cancellationTokenSource = new CancellationTokenSource();

PosixSignals.RegisterCancellation(cancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel server...");
});

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

	await Task.WhenAll(
		RpcLauncher.Launch(new RpcConfiguration(PhantomLogger.Create("Rpc"), Host: "0.0.0.0", Port: 9401, cancellationTokenSource.Token)),
		WebLauncher.Launch(new WebConfiguration(PhantomLogger.Create("Web"), Host: "0.0.0.0", Port: 9400, cancellationTokenSource.Token), options => options.UseNpgsql(connectionStringBuilder.ToString()))
	);
} finally {
	cancellationTokenSource.Dispose();
	PhantomLogger.Dispose();
}
