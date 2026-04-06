using MemoryPack;

namespace Phantom.Common.Data.Agent.Instance.Backups;

[MemoryPackable]
[MemoryPackUnion(tag: 0, type: typeof(InstancePlayerCountDetectionStrategy.MinecraftStatusProtocol))]
public partial interface IInstancePlayerCountDetectionStrategy {
	IInstancePlayerCountDetector CreateDetector(IInstancePlayerCountDetectorFactory factory);
}

public static partial class InstancePlayerCountDetectionStrategy {
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record MinecraftStatusProtocol(
		[property: MemoryPackOrder(0)] ushort Port
	) : IInstancePlayerCountDetectionStrategy {
		public IInstancePlayerCountDetector CreateDetector(IInstancePlayerCountDetectorFactory factory) {
			return factory.MinecraftStatusProtocol(Port);
		}
	}
}
