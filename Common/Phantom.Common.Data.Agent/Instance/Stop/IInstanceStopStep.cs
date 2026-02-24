using MemoryPack;

namespace Phantom.Common.Data.Agent.Instance.Stop;

[MemoryPackable]
[MemoryPackUnion(tag: 0, typeof(InstanceStopStep.Wait))]
[MemoryPackUnion(tag: 1, typeof(InstanceStopStep.SendToStandardInput))]
public partial interface IInstanceStopStep {
	Task<TResult> Run<TResult>(IInstanceStopStepExecutor<TResult> executor);
}

public static partial class InstanceStopStep {
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record Wait(
		[property: MemoryPackOrder(0)] TimeSpan Duration
	) : IInstanceStopStep {
		public Task<TResult> Run<TResult>(IInstanceStopStepExecutor<TResult> executor) {
			return executor.Wait(Duration);
		}
	}
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record SendToStandardInput(
		[property: MemoryPackOrder(0)] IInstanceValue Line
	) : IInstanceStopStep {
		public Task<TResult> Run<TResult>(IInstanceStopStepExecutor<TResult> executor) {
			return executor.SendToStandardInput(Line);
		}
	}
}
