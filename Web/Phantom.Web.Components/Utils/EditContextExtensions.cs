using Microsoft.AspNetCore.Components.Forms;

namespace Phantom.Web.Components.Utils; 

public static class EditContextExtensions {
	public static void RevalidateWhenFieldChanges(this EditContext editContext, string tracked, string revalidated) {
		editContext.OnFieldChanged += (_, args) => {
			if (args.FieldIdentifier.FieldName == tracked) {
				editContext.NotifyFieldChanged(editContext.Field(revalidated));
			}
		};
	}
}
