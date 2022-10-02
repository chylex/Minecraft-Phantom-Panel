using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Rpc;
using Phantom.Agent.Services.Instances.States;
using Phantom.Common.Data.Instance;
using Phantom.Common.Messages.ToServer;
using Serilog;

namespace Phantom.Agent.Services.Instances;

abstract class InstanceContext {
	public InstanceConfiguration Configuration { get; }
	public BaseLauncher Launcher { get; }

	public abstract LaunchServices LaunchServices { get; }
	public abstract PortManager PortManager { get; }
	public abstract ILogger Logger { get; }
	public abstract string ShortName { get; }

	protected InstanceContext(InstanceConfiguration configuration, BaseLauncher launcher) {
		Configuration = configuration;
		Launcher = launcher;
	}

	public abstract void TransitionState(Func<IInstanceState> newState);

	public void TransitionState(IInstanceState newState) {
		TransitionState(() => newState);
	}

	public void ReportStatus(InstanceStatus status) {
		Task.Run(() => ServerMessaging.SendMessage(new ReportInstanceStatusMessage(Configuration.InstanceGuid, status)));
	}
}
