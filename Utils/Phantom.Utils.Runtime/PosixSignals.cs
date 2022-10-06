using System.Runtime.InteropServices;

namespace Phantom.Utils.Runtime;

public static class PosixSignals {
	public static void RegisterCancellation(CancellationTokenSource cancellationTokenSource, Action? callback = null) {
		var shutdown = new CancellationCallback(cancellationTokenSource, callback).Run;
		PosixSignalRegistration.Create(PosixSignal.SIGINT, shutdown);
		PosixSignalRegistration.Create(PosixSignal.SIGTERM, shutdown);
		PosixSignalRegistration.Create(PosixSignal.SIGQUIT, shutdown);
	}

	private sealed class CancellationCallback {
		private readonly CancellationTokenSource cancellationTokenSource;
		private readonly Action? callback;
		
		public CancellationCallback(CancellationTokenSource cancellationTokenSource, Action? callback) {
			this.cancellationTokenSource = cancellationTokenSource;
			this.callback = callback;
		}

		public void Run(PosixSignalContext context) {
			context.Cancel = true;
			if (!cancellationTokenSource.IsCancellationRequested) {
				cancellationTokenSource.Cancel();
				callback?.Invoke();
			}
		}
	}
}
