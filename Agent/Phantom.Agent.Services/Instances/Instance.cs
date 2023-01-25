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

	public static async Task<Instance> Create(InstanceConfiguration configuration, BaseLauncher launcher, LaunchServices launchServices, PortManager portManager) {
		var instance = new Instance(configuration, launcher, launchServices, portManager);
		await instance.ReportLastStatus();
		return instance;
	}

	public InstanceConfiguration Configuration { get; private set; }
	private BaseLauncher Launcher { get; set; }

	private readonly string shortName;
	private readonly ILogger logger;

	private readonly LaunchServices launchServices;
	private readonly PortManager portManager;

	private IInstanceStatus currentStatus;
	private IInstanceState currentState;
	private readonly SemaphoreSlim stateTransitioningActionSemaphore = new (1, 1);

	private Instance(InstanceConfiguration configuration, BaseLauncher launcher, LaunchServices launchServices, PortManager portManager) {
		this.shortName = GetLoggerName(configuration.InstanceGuid);
		this.logger = PhantomLogger.Create<Instance>(shortName);

		this.Configuration = configuration;
		this.Launcher = launcher;

		this.launchServices = launchServices;
		this.portManager = portManager;
		this.currentState = new InstanceNotRunningState();
		this.currentStatus = InstanceStatus.NotRunning;
	}

	private async Task ReportLastStatus() {
		await ServerMessaging.Send(new ReportInstanceStatusMessage(Configuration.InstanceGuid, currentStatus));
	}
	
	private void TransitionState(IInstanceState newState) {
		if (currentState == newState) {
			return;
		}

		if (currentState is IDisposable disposable) {
			disposable.Dispose();
		}

		logger.Verbose("Transitioning instance state to: {NewState}", newState.GetType().Name);
		
		currentState = newState;
		currentState.Initialize();
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
			await ReportLastStatus();
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
		
		private int statusUpdateCounter;

		public InstanceContextImpl(Instance instance, CancellationToken shutdownCancellationToken) : base(instance.Configuration, instance.Launcher) {
			this.instance = instance;
			this.shutdownCancellationToken = shutdownCancellationToken;
		}

		public override LaunchServices LaunchServices => instance.launchServices;
		public override PortManager PortManager => instance.portManager;
		public override ILogger Logger => instance.logger;
		public override string ShortName => instance.shortName;

		public override void ReportStatus(IInstanceStatus newStatus) {
			int myStatusUpdateCounter = Interlocked.Increment(ref statusUpdateCounter);
			
			instance.launchServices.TaskManager.Run(async () => {
				if (myStatusUpdateCounter == statusUpdateCounter) {
					instance.currentStatus = newStatus;
					await ServerMessaging.Send(new ReportInstanceStatusMessage(Configuration.InstanceGuid, newStatus));
				}
			});
		}

		public override void TransitionState(Func<(IInstanceState, IInstanceStatus?)> newStateAndStatus) {
			instance.stateTransitioningActionSemaphore.Wait(CancellationToken.None);
			try {
				var (state, status) = newStateAndStatus();
				if (state is not InstanceNotRunningState && shutdownCancellationToken.IsCancellationRequested) {
					instance.logger.Verbose("Cancelled state transition to {State} due to Agent shutdown.", state.GetType().Name);
					return;
				}

				if (status != null) {
					ReportStatus(status);
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
		if (currentState is IDisposable disposable) {
			disposable.Dispose();
		}

		stateTransitioningActionSemaphore.Dispose();
	}
}
