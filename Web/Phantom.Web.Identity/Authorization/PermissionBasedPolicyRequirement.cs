using Microsoft.AspNetCore.Authorization;
using Phantom.Server.Web.Identity.Data;

namespace Phantom.Server.Web.Identity.Authorization; 

sealed record PermissionBasedPolicyRequirement(Permission Permission) : IAuthorizationRequirement;
