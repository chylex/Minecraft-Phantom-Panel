using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Phantom.Web.Components.Utils;

namespace Phantom.Web.Components.Forms;

public sealed class FormButtonSubmit : ComponentBase {
	[Parameter]
	public string Label { get; set; } = "Submit";
	
	[CascadingParameter]
	public Form? Form { get; set; }
	
	[Parameter]
	public SubmitModel? Model { get; set; }
	
	[Parameter(CaptureUnmatchedValues = true)]
	public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
	
	protected override void OnParametersSet() {
		BlazorUtils.RequireEitherParameterIsSet(Form, Model);
	}
	
	protected override void BuildRenderTree(RenderTreeBuilder builder) {
		var model = Form?.Model.SubmitModel ?? Model ?? throw new InvalidOperationException();
		
		builder.OpenElement(sequence: 0, "input");
		builder.AddMultipleAttributes(sequence: 1, AdditionalAttributes);
		builder.AddAttribute(sequence: 2, "type", "submit");
		
		string? cssClass = BlazorUtils.CombineClassNames(AdditionalAttributes, model.SubmitError == null ? null : "is-invalid");
		if (!string.IsNullOrEmpty(cssClass)) {
			builder.AddAttribute(sequence: 3, "class", cssClass);
		}
		
		builder.AddAttribute(sequence: 4, "disabled", BlazorUtils.CombineBooleansWithOr(AdditionalAttributes, "disabled", model.IsSubmitting));
		builder.AddAttribute(sequence: 5, "value", Label);
		builder.CloseElement();
	}
	
	public sealed class SubmitModel {
		public bool IsSubmitting { get; private set; }
		public string? SubmitError { get; private set; }
		
		public async Task StartSubmitting() {
			IsSubmitting = true;
			SubmitError = null;
			await Task.Yield();
		}
		
		public void StopSubmitting(string? error = null) {
			IsSubmitting = false;
			SubmitError = error;
		}
	}
}
