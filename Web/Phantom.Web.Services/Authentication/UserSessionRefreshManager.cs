using System.Collections.Concurrent;

namespace Phantom.Web.Services.Authentication;

public sealed class UserSessionRefreshManager {
	private readonly ConcurrentDictionary<Guid, EventHolder> userUpdateEventHoldersByUserGuid = new ();

	internal EventHolder GetEventHolder(Guid userGuid) {
		return userUpdateEventHoldersByUserGuid.GetOrAdd(userGuid, static _ => new EventHolder());
	}
	
	internal void RefreshUser(Guid userGuid) {
		if (userUpdateEventHoldersByUserGuid.TryGetValue(userGuid, out var eventHolder)) {
			eventHolder.Notify();
		}
	}
	
	internal sealed class EventHolder {
		public event EventHandler? UserNeedsRefresh;

		internal void Notify() {
			UserNeedsRefresh?.Invoke(null, EventArgs.Empty);
		}
	}
}
