using Microsoft.AspNetCore.Authorization;
using Phantom.Web.Identity.Data;

namespace Phantom.Web.Identity.Authorization;

sealed record PermissionBasedPolicyRequirement(Permission Permission) : IAuthorizationRequirement;
