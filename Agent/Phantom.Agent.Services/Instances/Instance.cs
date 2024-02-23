using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Services.Instances.Procedures;
using Phantom.Agent.Services.Instances.States;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Services.Instances;

sealed class Instance : IAsyncDisposable {
	private InstanceServices Services { get; }
	
	public InstanceConfiguration Configuration { get; private set; }
	private IServerLauncher Launcher { get; set; }
	private readonly SemaphoreSlim configurationSemaphore = new (1, 1);

	private readonly Guid instanceGuid;
	private readonly string shortName;
	private readonly ILogger logger;

	private IInstanceStatus currentStatus;

	private IInstanceState currentState;
	public bool IsRunning => currentState is not InstanceNotRunningState;
	
	public event EventHandler? IsRunningChanged;
	
	private readonly InstanceProcedureManager procedureManager;

	public Instance(Guid instanceGuid, string shortName, InstanceServices services, InstanceConfiguration configuration, IServerLauncher launcher) {
		this.instanceGuid = instanceGuid;
		this.shortName = shortName;
		this.logger = PhantomLogger.Create<Instance>(shortName);

		this.Services = services;
		this.Configuration = configuration;
		this.Launcher = launcher;
		
		this.currentStatus = InstanceStatus.NotRunning;
		this.currentState = new InstanceNotRunningState();
		
		this.procedureManager = new InstanceProcedureManager(this, new Context(this), services.TaskManager);
	}

	public void ReportLastStatus() {
		Services.ControllerConnection.Send(new ReportInstanceStatusMessage(instanceGuid, currentStatus));
	}

	private void ReportAndSetStatus(IInstanceStatus status) {
		currentStatus = status;
		Services.ControllerConnection.Send(new ReportInstanceStatusMessage(instanceGuid, status));
	}

	private void ReportEvent(IInstanceEvent instanceEvent) {
		Services.ControllerConnection.Send(new ReportInstanceEventMessage(Guid.NewGuid(), DateTime.UtcNow, instanceGuid, instanceEvent));
	}
	
	internal void TransitionState(IInstanceState newState) {
		if (currentState == newState) {
			return;
		}

		if (currentState is IDisposable disposable) {
			disposable.Dispose();
		}

		logger.Debug("Transitioning instance state to: {NewState}", newState.GetType().Name);
		
		var wasRunning = IsRunning;
		currentState = newState;
		currentState.Initialize();

		if (IsRunning != wasRunning) {
			IsRunningChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public async Task Reconfigure(InstanceConfiguration configuration, IServerLauncher launcher, CancellationToken cancellationToken) {
		await configurationSemaphore.WaitAsync(cancellationToken);
		try {
			Configuration = configuration;
			Launcher = launcher;
		} finally {
			configurationSemaphore.Release();
		}
	}

	public async Task<LaunchInstanceResult> Launch(CancellationToken cancellationToken) {
		if (IsRunning) {
			return LaunchInstanceResult.InstanceAlreadyRunning;
		}

		if (await procedureManager.GetCurrentProcedure(cancellationToken) is LaunchInstanceProcedure) {
			return LaunchInstanceResult.InstanceAlreadyLaunching;
		}
		
		LaunchInstanceProcedure procedure;
		
		await configurationSemaphore.WaitAsync(cancellationToken);
		try {
			procedure = new LaunchInstanceProcedure(instanceGuid, Configuration, Launcher);
		} finally {
			configurationSemaphore.Release();
		}
		
		ReportAndSetStatus(InstanceStatus.Launching);
		await procedureManager.Enqueue(procedure);
		return LaunchInstanceResult.LaunchInitiated;
	}

	public async Task<StopInstanceResult> Stop(MinecraftStopStrategy stopStrategy, CancellationToken cancellationToken) {
		if (!IsRunning) {
			return StopInstanceResult.InstanceAlreadyStopped;
		}
		
		if (await procedureManager.GetCurrentProcedure(cancellationToken) is StopInstanceProcedure) {
			return StopInstanceResult.InstanceAlreadyStopping;
		}
		
		ReportAndSetStatus(InstanceStatus.Stopping);
		await procedureManager.Enqueue(new StopInstanceProcedure(stopStrategy));
		return StopInstanceResult.StopInitiated;
	}

	public async Task<bool> SendCommand(string command, CancellationToken cancellationToken) {
		return await currentState.SendCommand(command, cancellationToken);
	}

	public async ValueTask DisposeAsync() {
		await procedureManager.DisposeAsync();

		while (currentState is not InstanceNotRunningState) {
			await Task.Delay(TimeSpan.FromMilliseconds(250), CancellationToken.None);
		}

		if (currentState is IDisposable disposable) {
			disposable.Dispose();
		}
		
		configurationSemaphore.Dispose();
	}

	private sealed class Context : IInstanceContext {
		public string ShortName => instance.shortName;
		public ILogger Logger => instance.logger;
		
		public InstanceServices Services => instance.Services;
		public IInstanceState CurrentState => instance.currentState;

		private readonly Instance instance;
		
		public Context(Instance instance) {
			this.instance = instance;
		}

		public void SetStatus(IInstanceStatus newStatus) {
			instance.ReportAndSetStatus(newStatus);
		}

		public void ReportEvent(IInstanceEvent instanceEvent) {
			instance.ReportEvent(instanceEvent);
		}

		public void EnqueueProcedure(IInstanceProcedure procedure, bool immediate) {
			Services.TaskManager.Run("Enqueue procedure for instance " + instance.shortName, () => instance.procedureManager.Enqueue(procedure, immediate));
		}
	}
}
