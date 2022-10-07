using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Phantom.Server.Web.Components.Utils;

namespace Phantom.Server.Web.Components.Forms;

public sealed class FormButtonSubmit : ComponentBase {
	[Parameter]
	public string Label { get; set; } = "Submit";

	[Parameter, EditorRequired]
	public SubmitModel Model { get; set; } = null!;

	[Parameter(CaptureUnmatchedValues = true)]
	public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

	protected override void BuildRenderTree(RenderTreeBuilder builder) {
		builder.OpenElement(0, "input");
		builder.AddMultipleAttributes(1, AdditionalAttributes);
		builder.AddAttribute(2, "type", "submit");

		string? cssClass = BlazorUtils.CombineClassNames(AdditionalAttributes, Model.SubmitError == null ? null : "is-invalid");
		if (!string.IsNullOrEmpty(cssClass)) {
			builder.AddAttribute(3, "class", cssClass);
		}
		
		builder.AddAttribute(4, "disabled", BlazorUtils.CombineBooleansWithOr(AdditionalAttributes, "disabled", Model.IsSubmitting));
		builder.AddAttribute(5, "value", Label);
		builder.CloseElement();
		
		builder.OpenElement(6, "div");
		builder.AddAttribute(7, "class", "invalid-feedback");
		builder.AddContent(8, Model.SubmitError);
		builder.CloseElement();
	}

	public sealed class SubmitModel {
		public bool IsSubmitting { get; private set; }
		public string? SubmitError { get; private set; }

		public void StartSubmitting() {
			IsSubmitting = true;
			SubmitError = null;
		}
		
		public void StopSubmitting(string? error = null) {
			IsSubmitting = false;
			SubmitError = error;
		}
	}
}
