using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Phantom.Web.Components.Utils;

namespace Phantom.Web.Components.Forms.Base;

public abstract class FormInputBaseDebounced<TValue> : FormInputBase<TValue>, IDisposable {
	private const uint DefaultDebounceMillis = 700;
	
	protected abstract ICustomFormField FormField { get; set; }
	
	[CascadingParameter]
	public EditContext EditContext { get; set; } = null!;
	
	[CascadingParameter]
	public Form? Form { get; set; }
	
	[Parameter]
	public bool DisableTwoWayBinding { get; set; }
	
	[Parameter]
	public uint DebounceMillis { get; set; } = DefaultDebounceMillis;
	
	private readonly DebounceTimer debounceTimer = new ();
	
	private string? debouncedValue;
	private bool debouncedValueIsSet = false;
	
	protected sealed override void OnInitialized() {
		debounceTimer.Fired += OnDebounceTimerFired;
		
		if (Form != null) {
			Form.BeforeSubmit += OnBeforeFormSubmit;
		}
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
	
	private void OnBeforeFormSubmit(object? sender, EventArgs e) {
		debounceTimer.Stop();
		SetDebouncedValue();
	}
	
	public void Dispose() {
		debounceTimer.Dispose();
	}
}
