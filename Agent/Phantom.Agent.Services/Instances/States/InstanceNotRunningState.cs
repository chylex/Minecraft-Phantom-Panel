using Phantom.Common.Data.Instance;

namespace Phantom.Agent.Services.Instances.States; 

sealed class InstanceNotRunningState : IInstanceState {
	public IInstanceState Launch(InstanceContext context) {
		InstanceLaunchFailReason? failReason = context.PortManager.Reserve(context.Configuration) switch {
			PortManager.Result.ServerPortNotAllowed   => InstanceLaunchFailReason.ServerPortNotAllowed,
			PortManager.Result.ServerPortAlreadyInUse => InstanceLaunchFailReason.ServerPortAlreadyInUse,
			PortManager.Result.RconPortNotAllowed     => InstanceLaunchFailReason.RconPortNotAllowed,
			PortManager.Result.RconPortAlreadyInUse   => InstanceLaunchFailReason.RconPortAlreadyInUse,
			_                                         => null
		};

		if (failReason != null) {
			context.ReportStatus(new InstanceStatus.Failed(failReason.Value));
			return this;
		}
		
		return new InstanceLaunchingState(context);
	}

	public IInstanceState Stop() {
		return this;
	}
}
