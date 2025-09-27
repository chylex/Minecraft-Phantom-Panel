using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Phantom.Web.Components.Forms.Base;

namespace Phantom.Web.Components.Forms.Fields;

public sealed class InputFieldNumeric<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : InputBase<TValue>, ICustomFormField {
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
		builder.OpenElement(sequence: 0, "input");
		builder.AddMultipleAttributes(sequence: 1, AdditionalAttributes);
		builder.AddAttribute(sequence: 2, "type", Type.GetHtmlInputType());
		
		if (!string.IsNullOrEmpty(CssClass)) {
			builder.AddAttribute(sequence: 3, "class", CssClass);
		}
		
		if (TwoWayValueBinding) {
			builder.AddAttribute(sequence: 4, "value", BindConverter.FormatValue(CurrentValueAsString));
		}
		
		builder.AddAttribute(sequence: 5, "onchange", OnChange);
		builder.AddAttribute(sequence: 6, "oninput", OnChange);
		builder.AddAttribute(sequence: 7, "onblur", OnBlur);
		builder.CloseElement();
	}
	
	protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage) {
		if (BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out result)) {
			validationErrorMessage = null;
			return true;
		}
		else {
			validationErrorMessage = string.Format(CultureInfo.InvariantCulture, ParsingErrorMessage, DisplayName ?? FieldIdentifier.FieldName);
			return false;
		}
	}
	
	protected override string? FormatValueAsString(TValue? value) {
		return value switch {
			null      => null,
			int v     => BindConverter.FormatValue(v, CultureInfo.InvariantCulture),
			long v    => BindConverter.FormatValue(v, CultureInfo.InvariantCulture),
			short v   => BindConverter.FormatValue(v, CultureInfo.InvariantCulture),
			float v   => BindConverter.FormatValue(v, CultureInfo.InvariantCulture),
			double v  => BindConverter.FormatValue(v, CultureInfo.InvariantCulture),
			decimal v => BindConverter.FormatValue(v, CultureInfo.InvariantCulture),
			ushort v  => BindConverter.FormatValue((int) v, CultureInfo.InvariantCulture),
			uint v    => BindConverter.FormatValue((long) v, CultureInfo.InvariantCulture),
			_         => throw new InvalidOperationException($"Unsupported value type {value.GetType()}"),
		};
	}
}
