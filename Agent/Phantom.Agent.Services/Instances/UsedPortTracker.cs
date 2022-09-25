using Phantom.Common.Data.Instance;

namespace Phantom.Agent.Services.Instances;

sealed class UsedPortTracker {
	private readonly HashSet<ushort> usedPorts = new ();

	public Result MarkUsed(InstanceInfo info) {
		lock (usedPorts) {
			if (usedPorts.Contains(info.ServerPort)) {
				return Result.ServerPortAlreadyInUse;
			}

			if (usedPorts.Contains(info.RconPort)) {
				return Result.RconPortAlreadyInUse;
			}

			usedPorts.Add(info.ServerPort);
			usedPorts.Add(info.RconPort);
		}

		return Result.Success;
	}
	
	public void Release(InstanceInfo info) {
		lock (usedPorts) {
			usedPorts.Remove(info.ServerPort);
			usedPorts.Remove(info.RconPort);
		}
	}

	public enum Result {
		Success,
		ServerPortAlreadyInUse,
		RconPortAlreadyInUse,
	}
}
