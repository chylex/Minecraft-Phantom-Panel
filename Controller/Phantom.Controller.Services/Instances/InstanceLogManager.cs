using System.Collections.Immutable;

namespace Phantom.Controller.Services.Instances;

sealed class InstanceLogManager {
	public sealed record Event(Guid InstanceGuid, ImmutableArray<string> Lines);
	
	public event EventHandler<Event>? LogsReceived; 

	internal void ReceiveLines(Guid instanceGuid, ImmutableArray<string> lines) {
		LogsReceived?.Invoke(this, new Event(instanceGuid, lines));
	}
}
