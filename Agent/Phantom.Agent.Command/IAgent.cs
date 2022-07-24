namespace Phantom.Agent.Command; 

public interface IAgent<TAgent, TCommandListener> where TAgent : IAgent<TAgent, TCommandListener> where TCommandListener : notnull {
	CommandListenerList<TAgent, TCommandListener> CommandListenerList { get; }
}
