using Phantom.Agent.Command;

namespace Phantom.Agent;

sealed class AgentServices : IAgent<AgentServices, CommandListener> {
	public InstanceManager InstanceManager { get; } = new ();

	public CommandListenerList<AgentServices, CommandListener> CommandListenerList { get; } = new ();
	public CommandQueue<AgentServices, CommandListener> CommandQueue { get; }

	public AgentServices() {
		CommandQueue = new CommandQueue<AgentServices, CommandListener>(this, workerCount: 4);
	}

	public async Task Shutdown() {
		await InstanceManager.StopAll();
		await CommandQueue.Shutdown();
	}
}
