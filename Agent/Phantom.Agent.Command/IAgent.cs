namespace Phantom.Agent.Command; 

public interface IAgent<TAgent, TCommandListener> where TAgent : IAgent<TAgent, TCommandListener> {
	CommandListenerList<TAgent, TCommandListener> CommandListenerList { get; }
}
