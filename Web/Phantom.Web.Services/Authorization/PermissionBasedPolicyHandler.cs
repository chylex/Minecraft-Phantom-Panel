using Microsoft.AspNetCore.Authorization;
using Phantom.Web.Services.Authentication;

namespace Phantom.Web.Services.Authorization;

sealed class PermissionBasedPolicyHandler : AuthorizationHandler<PermissionBasedPolicyRequirement> {
	protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionBasedPolicyRequirement requirement) {
		if (context.User.CheckPermission(requirement.Permission)) {
			context.Succeed(requirement);
		}
		else {
			context.Fail(new AuthorizationFailureReason(this, "Missing permission: " + requirement.Permission.Id));
		}
		
		return Task.CompletedTask;
	}
}
