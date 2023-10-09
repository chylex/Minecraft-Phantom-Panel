using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Phantom.Web.Components.Forms.Base;

public abstract class FormInputBase<TValue> : ComponentBase {
	[Parameter, EditorRequired]
	public string Id { get; set; } = null!;

	[Parameter]
	public string? Label { get; set; }

	[Parameter]
	public RenderFragment? LabelFragment { get; set; }

	[Parameter]
	public TValue? Value { get; set; }

	[Parameter]
	public EventCallback<TValue?> ValueChanged { get; set; }

	[Parameter]
	public Expression<Func<TValue?>>? ValueExpression { get; set; }

	[Parameter(CaptureUnmatchedValues = true)]
	public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

	protected IReadOnlyDictionary<string, object> GetAttributes(string cssClass) {
		Dictionary<string, object> result = new (2 + (AdditionalAttributes?.Count ?? 0)) {
			["id"] = Id,
			["class"] = cssClass
		};

		if (AdditionalAttributes != null) {
			foreach (var (key, value) in AdditionalAttributes) {
				result[key] = value;
			}
		}

		return result;
	}
}
