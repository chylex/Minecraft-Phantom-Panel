namespace Phantom.Agent.Command;

public abstract record Command<TAgent, TCommandListener, TResult> : ICommand<TAgent, TCommandListener> where TAgent : IAgent<TAgent, TCommandListener> where TCommandListener : notnull {
	async Task ICommand<TAgent, TCommandListener>.Run(TAgent agent) {
		TResult result = await Run(agent);
		agent.CommandListenerList.Report(this, result);
	}

	protected abstract Task<TResult> Run(TAgent agent);

	protected internal abstract void Report(TCommandListener listener, TResult result);
}
