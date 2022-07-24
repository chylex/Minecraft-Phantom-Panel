using Phantom.Utils.Threading;

namespace Phantom.Agent.Command;

public sealed class CommandQueue<TAgent, TCommandListener> where TAgent : IAgent<TAgent, TCommandListener> {
	private readonly TAgent agent;
	private readonly WorkerPool workerPool;

	public CommandQueue(TAgent agent, int workerCount) {
		this.agent = agent;
		this.workerPool = new WorkerPool(workerCount);
	}

	public void Add(ICommand<TAgent, TCommandListener> command) {
		workerPool.AddWork(() => command.Run(agent));
	}

	public async Task Shutdown() {
		await workerPool.Stop();
	}
}
