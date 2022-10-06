using Microsoft.Extensions.DependencyInjection;
using Phantom.Common.Data.Agent;
using Phantom.Server.Services;
using Phantom.Server.Services.Agents;
using Phantom.Server.Services.Rpc;
using WebLauncher = Phantom.Server.Web.Launcher;

namespace Phantom.Server;

sealed class WebConfigurator : WebLauncher.IConfigurator {
	private readonly AgentAuthToken agentToken;
	private readonly CancellationToken cancellationToken;

	public WebConfigurator(AgentAuthToken agentToken, CancellationToken cancellationToken) {
		this.agentToken = agentToken;
		this.cancellationToken = cancellationToken;
	}

	public void ConfigureServices(IServiceCollection services) {
		services.AddSingleton(new ServiceConfiguration(cancellationToken));
		services.AddSingleton(agentToken);
		services.AddSingleton<AgentManager>();
		services.AddSingleton<MessageToServerListenerFactory>();
	}

	public Task LoadFromDatabase(IServiceProvider serviceProvider) {
		return Task.CompletedTask;
	}
}
