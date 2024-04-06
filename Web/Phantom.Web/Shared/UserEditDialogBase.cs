using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Phantom.Common.Data.Web.Users;
using Phantom.Web.Components;
using Phantom.Web.Components.Forms;
using Phantom.Web.Services.Authentication;

namespace Phantom.Web.Shared;

public abstract class UserEditDialogBase : PhantomComponent {
	[Inject]
	public IJSRuntime Js { get; set; } = null!;

	[Parameter, EditorRequired]
	public string ModalId { get; set; } = string.Empty;

	[Parameter]
	public EventCallback<UserInfo> UserModified { get; set; }

	protected readonly FormButtonSubmit.SubmitModel SubmitModel = new ();

	private UserInfo? EditedUser { get; set; } = null;
	protected string EditedUserName { get; private set; } = string.Empty;

	internal async Task Show(UserInfo user) {
		EditedUser = user;
		EditedUserName = user.Name;
		await BeforeShown(user);

		StateHasChanged();
		await Js.InvokeVoidAsync("showModal", ModalId);
	}

	protected virtual Task BeforeShown(UserInfo user) {
		return Task.CompletedTask;
	}

	protected void OnClosed() {
		EditedUser = null;
	}

	protected async Task Submit() {
		await SubmitModel.StartSubmitting();

		if (EditedUser == null) {
			SubmitModel.StopSubmitting("Invalid user.");
			return;
		}

		switch (await DoEdit(await GetAuthenticatedUser(), EditedUser)) {
			case null:
				break;

			case UserActionFailure.NotAuthorized:
				SubmitModel.StopSubmitting("You do not have permission to edit users.");
				break;

			default:
				SubmitModel.StopSubmitting("Unknown error.");
				break;
		}
	}

	protected abstract Task<UserActionFailure?> DoEdit(AuthenticatedUser? authenticatedUser, UserInfo editedUser);

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
