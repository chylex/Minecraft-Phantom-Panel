using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Services.Instances.States;
using Phantom.Common.Data.Instance;
using Serilog;

namespace Phantom.Agent.Services.Instances;

abstract class InstanceContext {
	public InstanceConfiguration Configuration { get; }
	public InstanceServices Services { get; }
	public BaseLauncher Launcher { get; }
	
	public abstract ILogger Logger { get; }
	public abstract string ShortName { get; }

	protected InstanceContext(InstanceConfiguration configuration, InstanceServices services, BaseLauncher launcher) {
		Configuration = configuration;
		Launcher = launcher;
		Services = services;
	}

	public abstract void ReportStatus(IInstanceStatus newStatus);
	public abstract void TransitionState(Func<(IInstanceState, IInstanceStatus?)> newStateAndStatus);

	public void TransitionState(IInstanceState newState, IInstanceStatus? newStatus = null) {
		TransitionState(() => (newState, newStatus));
	}
}
