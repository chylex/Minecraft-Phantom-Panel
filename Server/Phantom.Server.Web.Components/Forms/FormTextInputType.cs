namespace Phantom.Server.Web.Components.Forms;

public enum FormTextInputType {
	Text,
	Password,
	Textarea
}

static class FormTextInputTypes {
	public static string GetHtmlInputType(this FormTextInputType type) {
		return type switch {
			FormTextInputType.Text     => "text",
			FormTextInputType.Password => "password",
			_                          => throw new InvalidOperationException($"Unsupported input type {type}")
		};
	}
}
