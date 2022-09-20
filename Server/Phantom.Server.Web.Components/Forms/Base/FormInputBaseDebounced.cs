using System.Timers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Timer = System.Timers.Timer;

namespace Phantom.Server.Web.Components.Forms.Base;

public abstract class FormInputBaseDebounced<TValue> : FormInputBase<TValue>, IDisposable {
	private const int DebounceTimeMillis = 700;
	
	protected abstract IStringConvertibleFormInput Input { get; set; }

	private string? debouncedValue;
	private bool debouncedValueIsSet = false;
	private Timer debounceTimer = null!;

	protected override void OnInitialized() {
		debounceTimer = new Timer(TimeSpan.FromMilliseconds(DebounceTimeMillis));
		debounceTimer.AutoReset = false;
		debounceTimer.Elapsed += OnDebounceTimerElapsed;
	}

	private void SetDebouncedValue() {
		if (debouncedValueIsSet) {
			Input.SetStringValue(debouncedValue);
			debouncedValueIsSet = false;
		}
	}

	private void OnDebounceTimerElapsed(object? sender, ElapsedEventArgs args) {
		InvokeAsync(SetDebouncedValue);
	}

	protected void OnChangeDebounced(ChangeEventArgs e) {
		debounceTimer.Stop();
		debouncedValue = (string?) e.Value;
		debouncedValueIsSet = true;
		debounceTimer.Start();
	}

	protected void OnBlur(FocusEventArgs e) {
		debounceTimer.Stop();
		SetDebouncedValue();
	}

	public void Dispose() {
		debounceTimer.Dispose();
	}
}
