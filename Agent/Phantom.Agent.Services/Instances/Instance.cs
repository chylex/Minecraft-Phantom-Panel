using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Rpc;
using Phantom.Agent.Services.Instances.States;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Phantom.Common.Messages.ToServer;
using Serilog;

namespace Phantom.Agent.Services.Instances;

sealed class Instance : IDisposable {
	private static uint loggerSequenceId = 0;

	private static string GetLoggerName(Guid guid) {
		var prefix = guid.ToString();
		return prefix[..prefix.IndexOf('-')] + "/" + Interlocked.Increment(ref loggerSequenceId);
	}

	public InstanceConfiguration Configuration { get; private set; }
	private InstanceServices Services { get; }
	private BaseLauncher Launcher { get; set; }

	private readonly string shortName;
	private readonly ILogger logger;

	private IInstanceStatus currentStatus;
	private int statusUpdateCounter;
	
	private IInstanceState currentState;
	private readonly SemaphoreSlim stateTransitioningActionSemaphore = new (1, 1);

	public bool IsRunning => currentState is not InstanceNotRunningState;
	
	public event EventHandler? IsRunningChanged; 

	public Instance(InstanceConfiguration configuration, InstanceServices services, BaseLauncher launcher) {
		this.shortName = GetLoggerName(configuration.InstanceGuid);
		this.logger = PhantomLogger.Create<Instance>(shortName);

		this.Configuration = configuration;
		this.Services = services;
		this.Launcher = launcher;
		
		this.currentState = new InstanceNotRunningState();
		this.currentStatus = InstanceStatus.NotRunning;
	}

	private void TryUpdateStatus(string taskName, Func<Task> getUpdateTask) {
		int myStatusUpdateCounter = Interlocked.Increment(ref statusUpdateCounter);
		
		Services.TaskManager.Run(taskName, async () => {
			if (myStatusUpdateCounter == statusUpdateCounter) {
				await getUpdateTask();
			}
		});
	}

	public void ReportLastStatus() {
		TryUpdateStatus("Report last status of instance " + shortName, async () => {
			await ServerMessaging.Send(new ReportInstanceStatusMessage(Configuration.InstanceGuid, currentStatus));
		});
	}

	private void ReportAndSetStatus(IInstanceStatus status) {
		TryUpdateStatus("Report status of instance " + shortName + " as " + status.GetType().Name, async () => {
			currentStatus = status;
			await ServerMessaging.Send(new ReportInstanceStatusMessage(Configuration.InstanceGuid, status));
		});
	}

	private void ReportEvent(IInstanceEvent instanceEvent) {
		var message = new ReportInstanceEventMessage(Guid.NewGuid(), DateTime.UtcNow, Configuration.InstanceGuid, instanceEvent);
		Services.TaskManager.Run("Report event for instance " + shortName, async () => await ServerMessaging.Send(message));
	}
	
	private void TransitionState(IInstanceState newState) {
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

	private T TransitionStateAndReturn<T>((IInstanceState State, T Result) newStateAndResult) {
		TransitionState(newStateAndResult.State);
		return newStateAndResult.Result;
	}

	public async Task Reconfigure(InstanceConfiguration configuration, BaseLauncher launcher, CancellationToken cancellationToken) {
		await stateTransitioningActionSemaphore.WaitAsync(cancellationToken);
		try {
			Configuration = configuration;
			Launcher = launcher;
		} finally {
			stateTransitioningActionSemaphore.Release();
		}
	}

	public async Task<LaunchInstanceResult> Launch(CancellationToken shutdownCancellationToken) {
		await stateTransitioningActionSemaphore.WaitAsync(shutdownCancellationToken);
		try {
			return TransitionStateAndReturn(currentState.Launch(new InstanceContextImpl(this, shutdownCancellationToken)));
		} catch (Exception e) {
			logger.Error(e, "Caught exception while launching instance.");
			return LaunchInstanceResult.UnknownError;
		} finally {
			stateTransitioningActionSemaphore.Release();
		}
	}

	public async Task<StopInstanceResult> Stop(MinecraftStopStrategy stopStrategy) {
		await stateTransitioningActionSemaphore.WaitAsync();
		try {
			return TransitionStateAndReturn(currentState.Stop(stopStrategy));
		} catch (Exception e) {
			logger.Error(e, "Caught exception while stopping instance.");
			return StopInstanceResult.UnknownError;
		} finally {
			stateTransitioningActionSemaphore.Release();
		}
	}

	public async Task StopAndWait(TimeSpan waitTime) {
		await Stop(MinecraftStopStrategy.Instant);

		using var waitTokenSource = new CancellationTokenSource(waitTime);
		var waitToken = waitTokenSource.Token;

		while (currentState is not InstanceNotRunningState) {
			await Task.Delay(TimeSpan.FromMilliseconds(250), waitToken);
		}
	}

	public async Task<bool> SendCommand(string command, CancellationToken cancellationToken) {
		return await currentState.SendCommand(command, cancellationToken);
	}

	private sealed class InstanceContextImpl : InstanceContext {
		private readonly Instance instance;
		private readonly CancellationToken shutdownCancellationToken;
		
		public InstanceContextImpl(Instance instance, CancellationToken shutdownCancellationToken) : base(instance.Configuration, instance.Launcher, instance.Services) {
			this.instance = instance;
			this.shutdownCancellationToken = shutdownCancellationToken;
		}

		public override ILogger Logger => instance.logger;
		public override string ShortName => instance.shortName;

		public override void SetStatus(IInstanceStatus newStatus) {
			instance.ReportAndSetStatus(newStatus);
		}

		public override void ReportEvent(IInstanceEvent instanceEvent) {
			instance.ReportEvent(instanceEvent);
		}

		public override void TransitionState(Func<(IInstanceState, IInstanceStatus?)> newStateAndStatus) {
			instance.stateTransitioningActionSemaphore.Wait(CancellationToken.None);
			try {
				var (state, status) = newStateAndStatus();
				
				if (!instance.IsRunning) {
					// Only InstanceSessionManager is allowed to transition an instance out of a non-running state.
					instance.logger.Debug("Cancelled state transition to {State} because instance is not running.", state.GetType().Name);
					return;
				}
				
				if (state is not InstanceNotRunningState && shutdownCancellationToken.IsCancellationRequested) {
					instance.logger.Debug("Cancelled state transition to {State} due to Agent shutdown.", state.GetType().Name);
					return;
				}

				if (status != null) {
					SetStatus(status);
				}

				instance.TransitionState(state);
			} catch (Exception e) {
				instance.logger.Error(e, "Caught exception during state transition.");
			} finally {
				instance.stateTransitioningActionSemaphore.Release();
			}
		}
	}

	public void Dispose() {
		stateTransitioningActionSemaphore.Dispose();
		
		if (currentState is IDisposable disposable) {
			disposable.Dispose();
		}
	}
}
