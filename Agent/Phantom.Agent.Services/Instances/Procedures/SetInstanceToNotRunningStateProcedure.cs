using Phantom.Agent.Services.Instances.States;
using Phantom.Common.Data.Instance;

namespace Phantom.Agent.Services.Instances.Procedures;

sealed record SetInstanceToNotRunningStateProcedure(IInstanceStatus Status) : IInstanceProcedure {
	public Task<IInstanceState?> Run(IInstanceContext context, CancellationToken cancellationToken) {
		if (context.CurrentState is InstanceRunningState { Process.HasEnded: true }) {
			context.SetStatus(Status);
			context.ReportEvent(InstanceEvent.Stopped);
			return Task.FromResult<IInstanceState?>(new InstanceNotRunningState());
		}
		else {
			return Task.FromResult<IInstanceState?>(null);
		}
	}
}
