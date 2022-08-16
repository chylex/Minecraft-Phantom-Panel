using Phantom.Utils.Collections;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Command;

public sealed class CommandListeners<TAgent, TCommandListener> where TCommandListener : notnull {
	private static readonly ILogger Logger = PhantomLogger.Create<CommandListeners<TAgent, TCommandListener>>();

	private readonly RwLockedList<TCommandListener> listeners = new (LockRecursionPolicy.SupportsRecursion);

	public void Add(TCommandListener listener) {
		listeners.Add(listener);
	}

	internal void Report<TCommand, TResult>(TCommand command, TResult result) where TCommand : Command<TAgent, TCommandListener, TResult> {
		listeners.ForEachWith((command, result), static (listener, o) => {
			try {
				o.command.Report(listener, o.result);
			} catch (Exception e) {
				Logger.Error(e, "Caught exception while reporting result for command {Command} to listener {Listener}. Commands and listeners are not supposed to throw exceptions!", o.command, listener.GetType().Name);
			}
		});
	}
}
