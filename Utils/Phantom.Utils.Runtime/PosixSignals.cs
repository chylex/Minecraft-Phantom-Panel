using System.Runtime.InteropServices;

namespace Phantom.Utils.Runtime;

public static class PosixSignals {
	public static void RegisterCancellation(CancellationTokenSource cancellationTokenSource, Action callback) {
		void Shutdown(PosixSignalContext context) {
			context.Cancel = true;
			if (!cancellationTokenSource.IsCancellationRequested) {
				cancellationTokenSource.Cancel();
				callback();
			}
		}

		PosixSignalRegistration.Create(PosixSignal.SIGINT, Shutdown);
		PosixSignalRegistration.Create(PosixSignal.SIGTERM, Shutdown);
		PosixSignalRegistration.Create(PosixSignal.SIGQUIT, Shutdown);
	}
}
