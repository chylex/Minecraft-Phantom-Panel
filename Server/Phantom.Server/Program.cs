using Phantom.Common.Logging;
using Phantom.Server;
using Phantom.Utils.Runtime;
using WebConfiguration = Phantom.Server.Web.Configuration;
using WebLauncher = Phantom.Server.Web.Launcher;

var cancellationTokenSource = new CancellationTokenSource();

PosixSignals.RegisterCancellation(cancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel server...");
});

try {
	PhantomLogger.Root.InformationHeading("Initializing Phantom Panel server...");
	
	var (webServerHost, webServerPort) = Variables.LoadOrExit();
	
	PhantomLogger.Root.InformationHeading("Launching Phantom Panel server...");

	var webConfiguration = new WebConfiguration(PhantomLogger.Create("Web"), webServerHost, webServerPort, cancellationTokenSource.Token);
	var webApplication = WebLauncher.CreateApplication(webConfiguration, new WebConfigurator());

	await WebLauncher.Launch(webConfiguration, webApplication);
} catch (OperationCanceledException) {
	// Ignore.
} finally {
	cancellationTokenSource.Dispose();
	PhantomLogger.Root.Information("Bye!");
	PhantomLogger.Dispose();
}
