using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Phantom.Server.Web.Components.Utils;

namespace Phantom.Server.Web.Components.Forms;

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
		
		builder.OpenElement(0, "input");
		builder.AddMultipleAttributes(1, AdditionalAttributes);
		builder.AddAttribute(2, "type", "submit");

		string? cssClass = BlazorUtils.CombineClassNames(AdditionalAttributes, model.SubmitError == null ? null : "is-invalid");
		if (!string.IsNullOrEmpty(cssClass)) {
			builder.AddAttribute(3, "class", cssClass);
		}
		
		builder.AddAttribute(4, "disabled", BlazorUtils.CombineBooleansWithOr(AdditionalAttributes, "disabled", model.IsSubmitting));
		builder.AddAttribute(5, "value", Label);
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
