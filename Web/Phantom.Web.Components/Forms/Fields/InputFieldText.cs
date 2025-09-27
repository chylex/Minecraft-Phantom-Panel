using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Phantom.Web.Components.Forms.Base;

namespace Phantom.Web.Components.Forms.Fields;

public sealed class InputFieldText : InputBase<string?>, ICustomFormField {
	[Parameter]
	public FormTextInputType Type { get; set; }
	
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
		if (Type == FormTextInputType.Textarea) {
			builder.OpenElement(sequence: 0, "textarea");
			builder.AddMultipleAttributes(sequence: 1, AdditionalAttributes);
		}
		else {
			builder.OpenElement(sequence: 0, "input");
			builder.AddMultipleAttributes(sequence: 1, AdditionalAttributes);
			builder.AddAttribute(sequence: 2, "type", Type.GetHtmlInputType());
		}
		
		if (!string.IsNullOrEmpty(CssClass)) {
			builder.AddAttribute(sequence: 3, "class", CssClass);
		}
		
		if (TwoWayValueBinding) {
			builder.AddAttribute(sequence: 4, "value", BindConverter.FormatValue(CurrentValue));
		}
		
		builder.AddAttribute(sequence: 5, "onchange", OnChange);
		builder.AddAttribute(sequence: 6, "oninput", OnChange);
		builder.AddAttribute(sequence: 7, "onblur", OnBlur);
		builder.CloseElement();
	}
	
	protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage) {
		result = value;
		validationErrorMessage = null;
		return true;
	}
}
