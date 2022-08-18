using Phantom.Agent.Command;
using Phantom.Agent.Services.Command;

namespace Phantom.Agent.Services;

public sealed class AgentServices {
	internal InstanceSessionManager InstanceSessionManager { get; } = new ();

	internal CommandListeners<AgentServices, CommandListener> CommandListeners { get; } = new ();
	internal CommandQueue<AgentServices, CommandListener> CommandQueue { get; }

	public AgentServices() {
		this.CommandQueue = new CommandQueue<AgentServices, CommandListener>(this, CommandListeners, workerCount: 4);
	}

	public async Task Shutdown() {
		await CommandQueue.Shutdown();
		await InstanceSessionManager.StopAll();
	}
}
