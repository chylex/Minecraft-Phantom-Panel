using Phantom.Common.Data;
using Phantom.Common.Data.Instance;

namespace Phantom.Agent.Services.Instances;

sealed class PortManager {
	private readonly AllowedPorts allowedServerPorts;
	private readonly AllowedPorts allowedRconPorts;
	private readonly HashSet<ushort> usedPorts = new ();

	public PortManager(AllowedPorts allowedServerPorts, AllowedPorts allowedRconPorts) {
		this.allowedServerPorts = allowedServerPorts;
		this.allowedRconPorts = allowedRconPorts;
	}

	public Result Reserve(InstanceConfiguration configuration) {
		var serverPort = configuration.ServerPort;
		var rconPort = configuration.RconPort;
		
		if (!allowedServerPorts.Contains(serverPort)) {
			return Result.ServerPortNotAllowed;
		}

		if (!allowedRconPorts.Contains(rconPort)) {
			return Result.RconPortNotAllowed;
		}
		
		lock (usedPorts) {
			if (usedPorts.Contains(serverPort)) {
				return Result.ServerPortAlreadyInUse;
			}

			if (usedPorts.Contains(rconPort)) {
				return Result.RconPortAlreadyInUse;
			}

			usedPorts.Add(serverPort);
			usedPorts.Add(rconPort);
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
		ServerPortNotAllowed,
		ServerPortAlreadyInUse,
		RconPortNotAllowed,
		RconPortAlreadyInUse
	}
}
