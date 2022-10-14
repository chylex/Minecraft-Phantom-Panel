using Microsoft.Extensions.DependencyInjection;
using Phantom.Common.Data.Agent;
using Phantom.Common.Minecraft;
using Phantom.Server.Services;
using Phantom.Server.Services.Agents;
using Phantom.Server.Services.Instances;
using Phantom.Server.Services.Rpc;
using Phantom.Utils.Threading;
using WebLauncher = Phantom.Server.Web.Launcher;

namespace Phantom.Server;

sealed class WebConfigurator : WebLauncher.IConfigurator {
	private readonly ServiceConfiguration serviceConfiguration;
	private readonly TaskManager taskManager;
	private readonly AgentAuthToken agentToken;

	public WebConfigurator(ServiceConfiguration serviceConfiguration, TaskManager taskManager, AgentAuthToken agentToken) {
		this.serviceConfiguration = serviceConfiguration;
		this.taskManager = taskManager;
		this.agentToken = agentToken;
	}

	public void ConfigureServices(IServiceCollection services) {
		services.AddSingleton(serviceConfiguration);
		services.AddSingleton(taskManager);
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
