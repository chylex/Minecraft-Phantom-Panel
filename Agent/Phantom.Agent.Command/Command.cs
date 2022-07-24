namespace Phantom.Agent.Command;

public abstract record Command<TAgent, TCommandListener, TResult> where TCommandListener : notnull {
	protected internal abstract Task<TResult> Run(TAgent agent);
	protected internal abstract void Report(TCommandListener listener, TResult result);
}
