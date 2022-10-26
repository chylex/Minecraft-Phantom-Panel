using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Phantom.Common.Logging;
using Phantom.Server.Web.Identity.Authorization;
using Phantom.Server.Web.Identity.Data;
using ILogger = Serilog.ILogger;

namespace Phantom.Server.Web.Base; 

public abstract class PhantomComponent : ComponentBase {
	private static readonly ILogger Logger = PhantomLogger.Create<PhantomComponent>();
	
	[CascadingParameter]
	public Task<AuthenticationState> AuthenticationStateTask { get; set; } = null!;

	[Inject]
	public PermissionManager PermissionManager { get; set; } = null!;

	protected async Task<bool> CheckPermission(Permission permission) {
		var authenticationState = await AuthenticationStateTask;
		return PermissionManager.CheckPermission(authenticationState.User, permission, refreshCache: true);
	}

	protected void InvokeAsyncChecked(Func<Task> task) {
		InvokeAsync(task).ContinueWith(static t => Logger.Error(t.Exception, "Caught exception in async task."), TaskContinuationOptions.OnlyOnFaulted);
	}
}
