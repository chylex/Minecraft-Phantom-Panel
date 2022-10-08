using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;

namespace Phantom.Agent.Services.Instances.States; 

sealed class InstanceNotRunningState : IInstanceState {
	public void Initialize() {}

	public (IInstanceState, LaunchInstanceResult) Launch(InstanceContext context) {
		InstanceLaunchFailReason? failReason = context.PortManager.Reserve(context.Configuration) switch {
			PortManager.Result.ServerPortNotAllowed   => InstanceLaunchFailReason.ServerPortNotAllowed,
			PortManager.Result.ServerPortAlreadyInUse => InstanceLaunchFailReason.ServerPortAlreadyInUse,
			PortManager.Result.RconPortNotAllowed     => InstanceLaunchFailReason.RconPortNotAllowed,
			PortManager.Result.RconPortAlreadyInUse   => InstanceLaunchFailReason.RconPortAlreadyInUse,
			_                                         => null
		};

		if (failReason != null) {
			context.ReportStatus(new InstanceStatus.Failed(failReason.Value));
			return (this, LaunchInstanceResult.LaunchInitiated);
		}
		
		return (new InstanceLaunchingState(context), LaunchInstanceResult.LaunchInitiated);
	}

	public (IInstanceState, StopInstanceResult) Stop() {
		return (this, StopInstanceResult.InstanceAlreadyStopped);
	}

	public Task<bool> SendCommand(string command, CancellationToken cancellationToken) {
		return Task.FromResult(false);
	}
}
