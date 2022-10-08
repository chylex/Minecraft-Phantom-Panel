using Microsoft.AspNetCore.Components.Forms;
using Phantom.Server.Web.Components.Utils;

namespace Phantom.Server.Web.Components.Forms; 

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
