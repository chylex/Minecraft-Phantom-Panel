using Microsoft.AspNetCore.Components.Forms;

namespace Phantom.Server.Web.Components.Utils;

static class BootstrapEditContext {
	public static EditContext Create(object model) {
		EditContext context = new EditContext(model);
		context.SetFieldCssClassProvider(ClassProvider);
		return context;
	}

	private static BootstrapFieldCssClassProvider ClassProvider { get; } = new ();

	private sealed class BootstrapFieldCssClassProvider : FieldCssClassProvider {
		public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier) {
			return editContext.GetValidationMessages(fieldIdentifier).Any() ? "is-invalid" : editContext.IsModified(fieldIdentifier) ? "is-valid" : "";
		}
	}
}
