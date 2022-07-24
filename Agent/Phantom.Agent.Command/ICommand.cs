namespace Phantom.Agent.Command;

public interface ICommand<TAgent, TCommandListener> where TAgent : IAgent<TAgent, TCommandListener> {
	Task Run(TAgent agent);
}
