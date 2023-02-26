using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Services.Instances.States;
using Phantom.Common.Data.Instance;
using Serilog;

namespace Phantom.Agent.Services.Instances;

abstract class InstanceContext {
	public InstanceServices Services { get; }
	public InstanceConfiguration Configuration { get; }
	public IServerLauncher Launcher { get; }
	
	public abstract ILogger Logger { get; }
	public abstract string ShortName { get; }

	protected InstanceContext(InstanceServices services, InstanceConfiguration configuration, IServerLauncher launcher) {
		Services = services;
		Configuration = configuration;
		Launcher = launcher;
	}

	public abstract void SetStatus(IInstanceStatus newStatus);

	public void SetLaunchFailedStatusAndReportEvent(InstanceLaunchFailReason reason) {
		SetStatus(InstanceStatus.Failed(reason));
		ReportEvent(new InstanceLaunchFailedEvent(reason));
	}

	public abstract void ReportEvent(IInstanceEvent instanceEvent);
	public abstract void TransitionState(Func<(IInstanceState, IInstanceStatus?)> newStateAndStatus);

	public void TransitionState(IInstanceState newState, IInstanceStatus? newStatus = null) {
		TransitionState(() => (newState, newStatus));
	}
}
