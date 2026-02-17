using System.Collections.Immutable;
using MemoryPack;

namespace Phantom.Common.Data.Agent.Instance.Launch;

[MemoryPackable]
[MemoryPackUnion(tag: 0, typeof(InstanceLaunchStep.DownloadFile))]
[MemoryPackUnion(tag: 1, typeof(InstanceLaunchStep.EditPropertiesFile))]
public partial interface IInstanceLaunchStep {
	Task<TResult> Run<TResult>(IInstanceLaunchStepExecutor<TResult> executor);
}

public static partial class InstanceLaunchStep {
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record DownloadFile(
		[property: MemoryPackOrder(0)] FileDownloadInfo DownloadInfo,
		[property: MemoryPackOrder(1)] IInstancePath Path
	) : IInstanceLaunchStep {
		public Task<TResult> Run<TResult>(IInstanceLaunchStepExecutor<TResult> executor) {
			return executor.DownloadFile(DownloadInfo, Path);
		}
	}
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record EditPropertiesFile(
		[property: MemoryPackOrder(0)] InstancePath.Local Path,
		[property: MemoryPackOrder(1)] string Comment,
		[property: MemoryPackOrder(2)] ImmutableDictionary<string, string> NewValues
	) : IInstanceLaunchStep {
		public Task<TResult> Run<TResult>(IInstanceLaunchStepExecutor<TResult> executor) {
			return executor.EditPropertiesFile(Path, Comment, NewValues);
		}
	}
}
