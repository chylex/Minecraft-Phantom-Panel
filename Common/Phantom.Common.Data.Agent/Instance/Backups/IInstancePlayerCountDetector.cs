using Phantom.Common.Data.Instance;

namespace Phantom.Common.Data.Agent.Instance.Backups;

public interface IInstancePlayerCountDetector {
	Task<InstancePlayerCounts?> TryGetPlayerCounts(CancellationToken cancellationToken);
}
