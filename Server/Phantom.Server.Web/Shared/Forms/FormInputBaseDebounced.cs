using System.Timers;
using Microsoft.AspNetCore.Components;
using Timer = System.Timers.Timer;

namespace Phantom.Server.Web.Shared.Forms;

public abstract class FormInputBaseDebounced<TValue> : FormInputBase<TValue>, IDisposable {
	private const int DebounceTimeMillis = 400;
	
	protected abstract IStringConvertibleFormInput Input { get; set; }

	private string? debouncedValue;
	private Timer debounceTimer = null!;

	protected override void OnInitialized() {
		debounceTimer = new Timer(TimeSpan.FromMilliseconds(DebounceTimeMillis));
		debounceTimer.AutoReset = false;
		debounceTimer.Elapsed += OnDebounceTimerElapsed;
	}

	private void OnDebounceTimerElapsed(object? sender, ElapsedEventArgs args) {
		InvokeAsync(() => {
			Input.SetStringValue(debouncedValue);
			ValueChanged.InvokeAsync(Value);
		});
	}

	protected void OnChangeDebounced(ChangeEventArgs e) {
		debounceTimer.Stop();
		debouncedValue = (string?) e.Value;
		debounceTimer.Start();
	}

	public void Dispose() {
		debounceTimer.Dispose();
	}
}
