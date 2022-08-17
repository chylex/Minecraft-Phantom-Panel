using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Phantom.Server.Web.Shared;

public sealed class InputRange<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : InputBase<TValue> {
	[Parameter]
	public string ParsingErrorMessage { get; set; } = "The {0} field must be a number.";

	protected override void BuildRenderTree(RenderTreeBuilder builder) {
		var eventCallback = EventCallback.Factory.CreateBinder<string?>(this, value => CurrentValueAsString = value, CurrentValueAsString);
		builder.OpenElement(0, "input");
		builder.AddMultipleAttributes(1, AdditionalAttributes);
		builder.AddAttribute(2, "type", "range");

		if (!string.IsNullOrEmpty(CssClass)) {
			builder.AddAttribute(3, "class", CssClass);
		}

		builder.AddAttribute(4, "value", BindConverter.FormatValue(CurrentValueAsString));
		builder.AddAttribute(5, "onchange", eventCallback);
		builder.AddAttribute(6, "oninput", eventCallback);
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
			_         => throw new InvalidOperationException($"Unsupported type {value.GetType()}")
		};
	}
}
