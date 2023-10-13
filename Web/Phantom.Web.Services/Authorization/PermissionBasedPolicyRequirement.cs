using Microsoft.AspNetCore.Authorization;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Web.Services.Authorization;

sealed record PermissionBasedPolicyRequirement(Permission Permission) : IAuthorizationRequirement;
