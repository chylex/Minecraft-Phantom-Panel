using Microsoft.Extensions.DependencyInjection;
using Phantom.Common.Data.Agent;
using Phantom.Controller.Minecraft;
using Phantom.Controller.Services;
using Phantom.Controller.Services.Agents;
using Phantom.Controller.Services.Audit;
using Phantom.Controller.Services.Events;
using Phantom.Controller.Services.Instances;
using Phantom.Controller.Services.Rpc;
using Phantom.Utils.Tasks;
using WebLauncher = Phantom.Web.Launcher;

namespace Phantom.Controller;

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
		services.AddSingleton<EventLog>();
		services.AddSingleton<InstanceManager>();
		services.AddSingleton<InstanceLogManager>();
		services.AddSingleton<MinecraftVersions>();
		services.AddSingleton<MessageToServerListenerFactory>();

		services.AddScoped<AuditLog>();
	}

	public async Task LoadFromDatabase(IServiceProvider serviceProvider) {
		await serviceProvider.GetRequiredService<AgentManager>().Initialize();
		await serviceProvider.GetRequiredService<InstanceManager>().Initialize();
	}
}
