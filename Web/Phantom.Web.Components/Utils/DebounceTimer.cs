using System.Timers;
using Timer = System.Timers.Timer;

namespace Phantom.Web.Components.Utils;

sealed class DebounceTimer : IDisposable {
	public event EventHandler? Fired;

	public uint Millis {
		get => millis;
		set {
			millis = value;

			if (millis == 0) {
				timer?.Dispose();
				timer = null;
			}
			else if (timer != null && Math.Abs(timer.Interval - millis) >= 1) {
				timer.Interval = millis;
			}
		}
	}

	private uint millis;

	private Timer? timer = null;

	public void Stop() {
		timer?.Stop();
	}

	public void Start() {
		if (Millis == 0) {
			Fired?.Invoke(this, EventArgs.Empty);
			return;
		}

		if (timer == null) {
			timer = new Timer(TimeSpan.FromMilliseconds(Millis));
			timer.AutoReset = false;
			timer.Elapsed += OnDebounceTimerElapsed;
		}

		timer.Start();
	}

	private void OnDebounceTimerElapsed(object? sender, ElapsedEventArgs args) {
		Fired?.Invoke(this, EventArgs.Empty);
	}

	public void Dispose() {
		timer?.Dispose();
	}
}
