using System.Runtime.InteropServices;

namespace Phantom.Utils.Runtime;

public static class PosixSignals {
	public static void RegisterCancellation(CancellationTokenSource cancellationTokenSource, Action? callback = null) {
		var cancellationCallback = new CancellationCallback(cancellationTokenSource, callback);
		var handlePosixSignal = cancellationCallback.HandlePosixSignal;
		PosixSignalRegistration.Create(PosixSignal.SIGINT, handlePosixSignal);
		PosixSignalRegistration.Create(PosixSignal.SIGTERM, handlePosixSignal);
		PosixSignalRegistration.Create(PosixSignal.SIGQUIT, handlePosixSignal);
		Console.CancelKeyPress += cancellationCallback.HandleConsoleCancel;
	}

	private sealed class CancellationCallback {
		private readonly CancellationTokenSource cancellationTokenSource;
		private readonly Action? callback;
		
		public CancellationCallback(CancellationTokenSource cancellationTokenSource, Action? callback) {
			this.cancellationTokenSource = cancellationTokenSource;
			this.callback = callback;
		}

		public void HandlePosixSignal(PosixSignalContext context) {
			context.Cancel = true;
			Run();
		}

		public void HandleConsoleCancel(object? sender, ConsoleCancelEventArgs e) {
			e.Cancel = true;
			Run();
		}

		private void Run() {
			lock (this) {
				if (!cancellationTokenSource.IsCancellationRequested) {
					cancellationTokenSource.Cancel();
					callback?.Invoke();
				}
			}
		}
	}
}
