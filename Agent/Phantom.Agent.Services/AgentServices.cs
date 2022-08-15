using Phantom.Agent.Command;
using Phantom.Agent.Services.Command;

namespace Phantom.Agent.Services;

public sealed class AgentServices {
	public InstanceManager InstanceManager { get; } = new ();

	public CommandListeners<AgentServices, CommandListener> CommandListeners { get; } = new ();
	public CommandQueue<AgentServices, CommandListener> CommandQueue { get; }

	public AgentServices() {
		CommandQueue = new CommandQueue<AgentServices, CommandListener>(this, CommandListeners, workerCount: 4);
	}

	public async Task Shutdown() {
		await CommandQueue.Shutdown();
		await InstanceManager.StopAll();
	}
}
