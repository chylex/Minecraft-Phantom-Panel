using Phantom.Agent.Services.Games;
using Phantom.Common.Data.Agent.Instance.Backups;

namespace Phantom.Agent.Services.Instances.State;

sealed class InstancePlayerCountDetectorFactory(InstanceContext instanceContext) : IInstancePlayerCountDetectorFactory {
	public IInstancePlayerCountDetector MinecraftStatusProtocol(ushort port) {
		return new MinecraftServerPlayerCountDetector(instanceContext, port);
	}
}
