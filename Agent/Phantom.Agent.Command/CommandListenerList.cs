namespace Phantom.Agent.Command; 

public sealed class CommandListenerList<TAgent, TCommandListener> where TAgent : IAgent<TAgent, TCommandListener> {
	private readonly List<TCommandListener> listeners = new ();
	private readonly ReaderWriterLock listenerLock = new ();
	
	public void Add(TCommandListener listener) {
		listenerLock.AcquireWriterLock(int.MaxValue);
		listeners.Add(listener);
		listenerLock.ReleaseWriterLock();
	}

	internal void Report<TCommand, TResult>(TCommand command, TResult result) where TCommand : Command<TAgent, TCommandListener, TResult> {
		listenerLock.AcquireReaderLock(int.MaxValue);
		
		foreach (var listener in listeners) {
			command.Report(listener, result);
		}

		listenerLock.ReleaseReaderLock();
	}
}
