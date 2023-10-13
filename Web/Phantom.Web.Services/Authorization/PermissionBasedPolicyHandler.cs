using Microsoft.AspNetCore.Authorization;

namespace Phantom.Web.Services.Authorization;

sealed class PermissionBasedPolicyHandler : AuthorizationHandler<PermissionBasedPolicyRequirement> {
	private readonly PermissionManager permissionManager;

	public PermissionBasedPolicyHandler(PermissionManager permissionManager) {
		this.permissionManager = permissionManager;
	}

	protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionBasedPolicyRequirement requirement) {
		if (permissionManager.CheckPermission(context.User, requirement.Permission)) {
			context.Succeed(requirement);
		}
		else {
			context.Fail(new AuthorizationFailureReason(this, "Missing permission: " + requirement.Permission.Id));
		}

		return Task.CompletedTask;
	}
}
