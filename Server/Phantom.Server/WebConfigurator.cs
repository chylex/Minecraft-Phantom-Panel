using Microsoft.Extensions.DependencyInjection;
using Phantom.Common.Data.Agent;
using Phantom.Common.Minecraft;
using Phantom.Server.Services;
using Phantom.Server.Services.Agents;
using Phantom.Server.Services.Instances;
using Phantom.Server.Services.Rpc;
using WebLauncher = Phantom.Server.Web.Launcher;

namespace Phantom.Server;

sealed class WebConfigurator : WebLauncher.IConfigurator {
	private readonly AgentAuthToken agentToken;
	private readonly ServiceConfiguration serviceConfiguration;

	public WebConfigurator(AgentAuthToken agentToken, ServiceConfiguration serviceConfiguration) {
		this.agentToken = agentToken;
		this.serviceConfiguration = serviceConfiguration;
	}

	public void ConfigureServices(IServiceCollection services) {
		services.AddSingleton(serviceConfiguration);
		services.AddSingleton(agentToken);
		services.AddSingleton<AgentManager>();
		services.AddSingleton<AgentJavaRuntimesManager>();
		services.AddSingleton<AgentStatsManager>();
		services.AddSingleton<InstanceManager>();
		services.AddSingleton<InstanceLogManager>();
		services.AddSingleton<MinecraftVersions>();
		services.AddSingleton<MessageToServerListenerFactory>();
	}

	public async Task LoadFromDatabase(IServiceProvider serviceProvider) {
		await serviceProvider.GetRequiredService<AgentManager>().Initialize();
		await serviceProvider.GetRequiredService<InstanceManager>().Initialize();
	}
}
