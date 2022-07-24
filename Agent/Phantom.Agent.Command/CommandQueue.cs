using Phantom.Utils.Logging;
using Phantom.Utils.Threading;
using Serilog;

namespace Phantom.Agent.Command;

public sealed class CommandQueue<TAgent, TCommandListener> where TAgent : IAgent<TAgent, TCommandListener> {
	private readonly TAgent agent;
	private readonly WorkerPool workerPool;
	private readonly ILogger logger;

	public CommandQueue(TAgent agent, int workerCount) {
		this.agent = agent;
		this.workerPool = new WorkerPool(workerCount);
		this.logger = PhantomLogger.Create<CommandQueue<TAgent, TCommandListener>>();
	}

	public void Add(ICommand<TAgent, TCommandListener> command) {
		workerPool.AddWork(() => RunCommand(command));
	}

	private async Task RunCommand(ICommand<TAgent, TCommandListener> command) {
		logger.Debug("Running command: {Name}", command.GetType());
		
		try {
			await command.Run(agent);
		} catch (Exception e) {
			logger.Error(e, "Caught exception while running command {Name}. Commands are not supposed to throw exceptions!", command.GetType());
		}
	}

	public async Task Shutdown() {
		await workerPool.Stop();
	}
}
