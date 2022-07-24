namespace Phantom.Agent.Command;

public interface ICommand<TAgent, TCommandListener> where TAgent : IAgent<TAgent, TCommandListener> where TCommandListener : notnull {
	Task Run(TAgent agent);
}
