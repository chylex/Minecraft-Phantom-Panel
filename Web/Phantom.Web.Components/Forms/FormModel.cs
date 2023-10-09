using Microsoft.AspNetCore.Components.Forms;
using Phantom.Web.Components.Utils;

namespace Phantom.Web.Components.Forms; 

public abstract class FormModel {
	public EditContext EditContext { get; }
	public FormButtonSubmit.SubmitModel SubmitModel { get; } = new ();

	protected FormModel() {
		EditContext = BootstrapEditContext.Create(this);
	}
	
	protected FormModel(EditContext editContext) {
		EditContext = editContext;
	}
}
