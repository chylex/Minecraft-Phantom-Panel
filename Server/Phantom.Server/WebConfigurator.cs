using Microsoft.Extensions.DependencyInjection;
using WebLauncher = Phantom.Server.Web.Launcher;

namespace Phantom.Server;

sealed class WebConfigurator : WebLauncher.IConfigurator {
	public void ConfigureServices(IServiceCollection services) {}
}
