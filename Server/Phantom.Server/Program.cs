using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Phantom.Common.Logging;
using Phantom.Server;
using Phantom.Server.Database.Postgres;
using Phantom.Server.Rpc;
using Phantom.Server.Services;
using Phantom.Server.Services.Agents;
using Phantom.Server.Services.Instances;
using Phantom.Server.Services.Rpc;
using Phantom.Utils.IO;
using Phantom.Utils.Rpc;
using Phantom.Utils.Runtime;
using WebConfiguration = Phantom.Server.Web.Configuration;
using WebLauncher = Phantom.Server.Web.Launcher;

var cancellationTokenSource = new CancellationTokenSource();

PosixSignals.RegisterCancellation(cancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel server...");
});

try {
	var (webServerHost, webServerPort, rpcServerHost, rpcServerPort, sqlConnectionString) = Variables.LoadOrExit();
	PhantomLogger.Root.InformationHeading("Launching Phantom Panel server...");

	string secretsPath = Path.GetFullPath("./secrets");
	if (!Directory.Exists(secretsPath)) {
		try {
			Directories.Create(secretsPath, Chmod.URW_GR);
		} catch (Exception e) {
			PhantomLogger.Root.Fatal(e, "Error creating secrets folder.");
			Environment.Exit(1);
		}
	}

	var agentToken = await AgentTokenFile.CreateOrLoad(secretsPath);
	if (agentToken == null) {
		Environment.Exit(1);
	}

	var certificate = await CertificateFiles.CreateOrLoad(secretsPath);
	if (certificate == null) {
		Environment.Exit(1);
	}

	var rpcConfiguration = new RpcConfiguration(PhantomLogger.Create("Rpc"), rpcServerHost, rpcServerPort, certificate, cancellationTokenSource.Token);
	var webConfiguration = new WebConfiguration(PhantomLogger.Create("Web"), webServerHost, webServerPort, cancellationTokenSource.Token);

	var builder = WebLauncher.CreateBuilder(webConfiguration, options => options.UseNpgsql(sqlConnectionString, static options => {
		options.CommandTimeout(10).MigrationsAssembly(typeof(ApplicationDbContextDesignFactory).Assembly.FullName);
	}));
	
	var services = builder.Services;
	
	services.AddSingleton(new ServiceConfiguration(cancellationTokenSource.Token));
	services.AddSingleton(agentToken);
	services.AddSingleton<AgentManager>();
	services.AddSingleton<AgentStatsManager>();
	services.AddSingleton<InstanceManager>();
	
	var app = builder.Build();
	var agentManager = app.Services.GetRequiredService<AgentManager>();

	await Task.WhenAll(
		RpcLauncher.Launch(rpcConfiguration, connection => new MessageToServerListener(connection, agentManager)),
		WebLauncher.Launch(webConfiguration, app)
	);
	
	PhantomLogger.Root.Information("Bye!");
} finally {
	cancellationTokenSource.Dispose();
	PhantomLogger.Dispose();
}
