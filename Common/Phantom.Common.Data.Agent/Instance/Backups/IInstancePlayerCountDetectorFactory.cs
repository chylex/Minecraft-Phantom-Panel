namespace Phantom.Common.Data.Agent.Instance.Backups;

public interface IInstancePlayerCountDetectorFactory {
	IInstancePlayerCountDetector MinecraftStatusProtocol(ushort port);
}
