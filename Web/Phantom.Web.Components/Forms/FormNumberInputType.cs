namespace Phantom.Web.Components.Forms;

public enum FormNumberInputType {
	Number,
	Range
}

static class FormNumberInputTypes {
	public static string GetHtmlInputType(this FormNumberInputType type) {
		return type switch {
			FormNumberInputType.Number => "number",
			FormNumberInputType.Range  => "range",
			_                          => throw new InvalidOperationException($"Unsupported input type {type}")
		};
	}

	public static string GetBootstrapCssClass(this FormNumberInputType type) {
		return type switch {
			FormNumberInputType.Number => "form-control",
			FormNumberInputType.Range  => "form-range",
			_                          => throw new InvalidOperationException($"Unsupported input type {type}")
		};
	}
}
