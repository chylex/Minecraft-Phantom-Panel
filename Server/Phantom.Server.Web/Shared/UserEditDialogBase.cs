using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using Phantom.Server.Web.Base;
using Phantom.Server.Web.Components.Forms;
using Phantom.Server.Web.Identity.Data;

namespace Phantom.Server.Web.Shared;

public abstract class UserEditDialogBase : PhantomComponent {
	[Inject]
	public IJSRuntime Js { get; set; } = null!;

	[Parameter, EditorRequired]
	public string ModalId { get; set; } = string.Empty;

	[Parameter]
	public EventCallback<IdentityUser> UserModified { get; set; }

	protected readonly FormButtonSubmit.SubmitModel SubmitModel = new();

	private IdentityUser? EditedUser { get; set; } = null;
	protected string EditedUserName { get; private set; } = string.Empty;

	internal async Task Show(IdentityUser user) {
		EditedUser = user;
		EditedUserName = user.UserName ?? $"<{user.Id}>";
		await BeforeShown(user);

		StateHasChanged();
		await Js.InvokeVoidAsync("showModal", ModalId);
	}

	protected virtual Task BeforeShown(IdentityUser user) {
		return Task.CompletedTask;
	}

	protected void OnClosed() {
		EditedUser = null;
	}

	protected async Task Submit() {
		await SubmitModel.StartSubmitting();

		if (!await CheckPermission(Permission.EditUsers)) {
			SubmitModel.StopSubmitting("You do not have permission to edit users.");
		}
		else if (EditedUser == null) {
			SubmitModel.StopSubmitting("Invalid user.");
		}
		else {
			await DoEdit(EditedUser);
		}
	}
	
	protected abstract Task DoEdit(IdentityUser user);

	protected async Task OnEditSuccess() {
		await UserModified.InvokeAsync(EditedUser);
		await Js.InvokeVoidAsync("closeModal", ModalId);
		SubmitModel.StopSubmitting();
		OnClosed();
	}
	
	protected void OnEditFailure(string message) {
		SubmitModel.StopSubmitting(message);
	}
}
