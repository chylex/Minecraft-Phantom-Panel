using Phantom.Common.Logging;
using Phantom.Utils.Threading;
using Serilog;

namespace Phantom.Agent.Command;

public sealed class CommandQueue<TAgent, TCommandListener> where TCommandListener : notnull {
	private static readonly ILogger Logger = PhantomLogger.Create<CommandQueue<TAgent, TCommandListener>>();

	private readonly TAgent agent;
	private readonly CommandListeners<TAgent, TCommandListener> listeners;
	private readonly WorkerPool workerPool;

	public CommandQueue(TAgent agent, CommandListeners<TAgent, TCommandListener> listeners, int workerCount) {
		this.agent = agent;
		this.listeners = listeners;
		this.workerPool = new WorkerPool(workerCount);
	}

	public void Add<TResult>(Command<TAgent, TCommandListener, TResult> command) {
		workerPool.AddWork(() => RunCommand(command));
	}

	private async Task RunCommand<TResult>(Command<TAgent, TCommandListener, TResult> command) {
		Logger.Debug("Running command: {Command}", command);

		try {
			TResult result = await command.Run(agent);
			listeners.Report(command, result);
		} catch (Exception e) {
			Logger.Error(e, "Caught exception while running command {Command}. Commands are not supposed to throw exceptions!", command);
		}
	}

	public async Task Shutdown() {
		await workerPool.Stop();
	}
}
