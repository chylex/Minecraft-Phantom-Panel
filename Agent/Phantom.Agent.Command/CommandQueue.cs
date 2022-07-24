using Phantom.Utils.Logging;
using Phantom.Utils.Threading;
using Serilog;

namespace Phantom.Agent.Command;

public sealed class CommandQueue<TAgent, TCommandListener> where TAgent : IAgent<TAgent, TCommandListener> where TCommandListener : notnull {
	private static readonly ILogger Logger = PhantomLogger.Create<CommandQueue<TAgent, TCommandListener>>();
	
	private readonly TAgent agent;
	private readonly WorkerPool workerPool;

	public CommandQueue(TAgent agent, int workerCount) {
		this.agent = agent;
		this.workerPool = new WorkerPool(workerCount);
	}

	public void Add(ICommand<TAgent, TCommandListener> command) {
		workerPool.AddWork(() => RunCommand(command));
	}

	private async Task RunCommand(ICommand<TAgent, TCommandListener> command) {
		Logger.Debug("Running command: {Command}", command);
		
		try {
			await command.Run(agent);
		} catch (Exception e) {
			Logger.Error(e, "Caught exception while running command {Command}. Commands are not supposed to throw exceptions!", command);
		}
	}

	public async Task Shutdown() {
		await workerPool.Stop();
	}
}
