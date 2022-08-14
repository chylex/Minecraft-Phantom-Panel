using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Command;

public sealed class CommandListeners<TAgent, TCommandListener> where TCommandListener : notnull {
	private static readonly ILogger Logger = PhantomLogger.Create<CommandListeners<TAgent, TCommandListener>>();

	private readonly List<TCommandListener> listeners = new ();
	private readonly ReaderWriterLockSlim listenerLock = new (LockRecursionPolicy.SupportsRecursion);

	public void Add(TCommandListener listener) {
		listenerLock.EnterWriteLock();
		listeners.Add(listener);
		listenerLock.ExitWriteLock();
	}

	internal void Report<TCommand, TResult>(TCommand command, TResult result) where TCommand : Command<TAgent, TCommandListener, TResult> {
		listenerLock.EnterUpgradeableReadLock();

		try {
			foreach (var listener in listeners) {
				try {
					command.Report(listener, result);
				} catch (Exception e) {
					Logger.Error(e, "Caught exception while reporting result for command {Command} to listener {Listener}. Commands and listeners are not supposed to throw exceptions!", command, listener.GetType().Name);
				}
			}
		} finally {
			listenerLock.EnterUpgradeableReadLock();
		}
	}
}
