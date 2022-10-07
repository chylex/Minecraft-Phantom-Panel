using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Phantom.Server.Web.Components.Forms.Base;

namespace Phantom.Server.Web.Components.Forms.Fields;

public sealed class InputFieldText : InputBase<string?>, ICustomFormField {
	[Parameter]
	public FormNumberInputType Type { get; set; }

	[Parameter]
	public EventCallback<ChangeEventArgs> OnChange { get; set; }

	[Parameter]
	public EventCallback<FocusEventArgs> OnBlur { get; set; }

	[Parameter]
	public string ParsingErrorMessage { get; set; } = "The {0} field must be a number.";

	public bool TwoWayValueBinding { get; set; } = true;

	public void SetStringValue(string? value) {
		CurrentValueAsString = value;
	}

	protected override void BuildRenderTree(RenderTreeBuilder builder) {
		builder.OpenElement(0, "input");
		builder.AddMultipleAttributes(1, AdditionalAttributes);
		builder.AddAttribute(2, "type", "text");

		if (!string.IsNullOrEmpty(CssClass)) {
			builder.AddAttribute(3, "class", CssClass);
		}

		if (TwoWayValueBinding) {
			builder.AddAttribute(4, "value", BindConverter.FormatValue(CurrentValue));
		}

		builder.AddAttribute(5, "onchange", OnChange);
		builder.AddAttribute(6, "oninput", OnChange);
		builder.AddAttribute(7, "onblur", OnBlur);
		builder.CloseElement();
	}
	
	protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage) {
		result = value;
		validationErrorMessage = null;
		return true;
	}
}
