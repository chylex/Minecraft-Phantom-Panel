using System.Collections.Immutable;
using Phantom.Common.Data.Web.Users;

namespace Phantom.Controller.Services.Users.Sessions;

readonly record struct LoggedInUser(AuthenticatedUserInfo? AuthenticatedUserInfo) {
	public Guid? Guid => AuthenticatedUserInfo?.Guid;
	
	public bool CheckPermission(Permission permission) {
		return AuthenticatedUserInfo is {} info && info.CheckPermission(permission);
	}
	
	public bool HasAccessToAgent(Guid agentGuid) {
		return AuthenticatedUserInfo is {} info && info.HasAccessToAgent(agentGuid);
	}
	
	public ImmutableHashSet<Guid> FilterAccessibleAgentGuids(ImmutableHashSet<Guid> agentGuids) {
		return AuthenticatedUserInfo is {} info ? info.FilterAccessibleAgentGuids(agentGuids) : ImmutableHashSet<Guid>.Empty;
	}
}
