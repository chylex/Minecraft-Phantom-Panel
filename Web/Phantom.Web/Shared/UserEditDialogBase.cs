using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Phantom.Controller.Database.Entities;
using Phantom.Web.Base;
using Phantom.Web.Components.Forms;
using Phantom.Web.Identity.Data;

namespace Phantom.Web.Shared;

public abstract class UserEditDialogBase : PhantomComponent {
	[Inject]
	public IJSRuntime Js { get; set; } = null!;

	[Parameter, EditorRequired]
	public string ModalId { get; set; } = string.Empty;

	[Parameter]
	public EventCallback<UserEntity> UserModified { get; set; }

	protected readonly FormButtonSubmit.SubmitModel SubmitModel = new();

	private UserEntity? EditedUser { get; set; } = null;
	protected string EditedUserName { get; private set; } = string.Empty;

	internal async Task Show(UserEntity user) {
		EditedUser = user;
		EditedUserName = user.Name;
		await BeforeShown(user);

		StateHasChanged();
		await Js.InvokeVoidAsync("showModal", ModalId);
	}

	protected virtual Task BeforeShown(UserEntity user) {
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
	
	protected abstract Task DoEdit(UserEntity user);

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
