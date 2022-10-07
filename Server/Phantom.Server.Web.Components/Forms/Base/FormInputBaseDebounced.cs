using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Phantom.Server.Web.Components.Utils;

namespace Phantom.Server.Web.Components.Forms.Base;

public abstract class FormInputBaseDebounced<TValue> : FormInputBase<TValue>, IDisposable {
	private const uint DefaultDebounceMillis = 700;
	
	protected abstract ICustomFormField FormField { get; set; }

	[Parameter]
	public bool DisableTwoWayBinding { get; set; }

	[Parameter]
	public uint DebounceMillis { get; set; } = DefaultDebounceMillis;

	private readonly DebounceTimer debounceTimer = new ();

	private string? debouncedValue;
	private bool debouncedValueIsSet = false;
	
	protected sealed override void OnInitialized() {
		debounceTimer.Fired += OnDebounceTimerFired;
	}

	protected override void OnParametersSet() {
		debounceTimer.Millis = DebounceMillis;
	}

	private void SetDebouncedValue() {
		if (debouncedValueIsSet) {
			FormField.SetStringValue(debouncedValue);
			debouncedValueIsSet = false;
		}
	}

	private void OnDebounceTimerFired(object? sender, EventArgs e) {
		InvokeAsync(SetDebouncedValue);
	}

	protected void OnChangeDebounced(ChangeEventArgs e) {
		if (DisableTwoWayBinding) {
			FormField.TwoWayValueBinding = false;
		}
		
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
