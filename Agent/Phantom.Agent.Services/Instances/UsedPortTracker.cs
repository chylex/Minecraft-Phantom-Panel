using Phantom.Common.Data.Instance;

namespace Phantom.Agent.Services.Instances;

sealed class UsedPortTracker {
	private readonly HashSet<ushort> usedPorts = new ();

	public Result Reserve(InstanceConfiguration configuration) {
		lock (usedPorts) {
			if (usedPorts.Contains(configuration.ServerPort)) {
				return Result.ServerPortAlreadyInUse;
			}

			if (usedPorts.Contains(configuration.RconPort)) {
				return Result.RconPortAlreadyInUse;
			}

			usedPorts.Add(configuration.ServerPort);
			usedPorts.Add(configuration.RconPort);
		}

		return Result.Success;
	}
	
	public void Release(InstanceConfiguration configuration) {
		lock (usedPorts) {
			usedPorts.Remove(configuration.ServerPort);
			usedPorts.Remove(configuration.RconPort);
		}
	}

	public enum Result {
		Success,
		ServerPortAlreadyInUse,
		RconPortAlreadyInUse,
	}
}
