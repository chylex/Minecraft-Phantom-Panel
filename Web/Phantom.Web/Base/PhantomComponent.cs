using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Phantom.Common.Data.Web.Users;
using Phantom.Utils.Logging;
using Phantom.Web.Services.Authorization;
using ILogger = Serilog.ILogger;
using UserInfo = Phantom.Web.Services.Authentication.UserInfo;

namespace Phantom.Web.Base;

public abstract class PhantomComponent : ComponentBase, IDisposable {
	private static readonly ILogger Logger = PhantomLogger.Create<PhantomComponent>();
	
	[CascadingParameter]
	public Task<AuthenticationState> AuthenticationStateTask { get; set; } = null!;

	[Inject]
	public PermissionManager PermissionManager { get; set; } = null!;

	private readonly CancellationTokenSource cancellationTokenSource = new ();

	protected CancellationToken CancellationToken => cancellationTokenSource.Token;

	protected async Task<Guid?> GetUserGuid() {
		var authenticationState = await AuthenticationStateTask;
		return UserInfo.TryGetGuid(authenticationState.User);
	}

	protected async Task<bool> CheckPermission(Permission permission) {
		var authenticationState = await AuthenticationStateTask;
		return PermissionManager.CheckPermission(authenticationState.User, permission);
	}

	protected void InvokeAsyncChecked(Func<Task> task) {
		InvokeAsync(task).ContinueWith(static t => Logger.Error(t.Exception, "Caught exception in async task."), TaskContinuationOptions.OnlyOnFaulted);
	}

	public void Dispose() {
		cancellationTokenSource.Cancel();
		cancellationTokenSource.Dispose();
		OnDisposed();
		GC.SuppressFinalize(this);
	}

	protected virtual void OnDisposed() {}
}
