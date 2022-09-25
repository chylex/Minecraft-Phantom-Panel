using Phantom.Agent.Minecraft.Instance;
using Phantom.Agent.Minecraft.Launcher;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Logging;
using Serilog;

namespace Phantom.Agent.Services.Instances;

sealed class Instance : IDisposable {
	private static uint loggerSequenceId = 0;

	private static string GetLoggerName(Guid guid) {
		var prefix = guid.ToString();
		return prefix[..prefix.IndexOf('-')] + "/" + Interlocked.Increment(ref loggerSequenceId);
	}
	
	public InstanceInfo Info { get; }

	private readonly string shortName;
	private readonly ILogger logger;
	
	private readonly BaseLauncher launcher;
	private readonly LaunchServices launchServices;
	private readonly UsedPortTracker usedPortTracker;

	private IState currentState;
	private readonly SemaphoreSlim stateTransitioningActionSemaphore = new (1, 1);

	public Instance(InstanceInfo info, BaseLauncher launcher, LaunchServices launchServices, UsedPortTracker usedPortTracker) {
		this.shortName = GetLoggerName(info.InstanceGuid);
		this.logger = PhantomLogger.Create<Instance>(shortName);

		this.Info = info;
		this.launcher = launcher;
		this.launchServices = launchServices;
		this.usedPortTracker = usedPortTracker;
		this.currentState = new NotRunningState(this);
	}

	private bool TransitionState(IState newState) {
		if (currentState == newState) {
			return false;
		}

		if (currentState is IDisposable disposable) {
			disposable.Dispose();
		}

		currentState = newState;
		return true;
	}

	public async Task<LaunchInstanceResult> Launch(CancellationToken cancellationToken) {
		await stateTransitioningActionSemaphore.WaitAsync(cancellationToken);
		try {
			var (state, result) = await currentState.Launch(cancellationToken);
			TransitionState(state);
			return result;
		} finally {
			stateTransitioningActionSemaphore.Release();
		}
	}

	public async Task<bool> SendCommand(string command, CancellationToken cancellationToken) {
		return await currentState.SendCommand(command, cancellationToken);
	}

	public async Task Stop(TimeSpan waitTime) {
		await stateTransitioningActionSemaphore.WaitAsync(waitTime);
		try {
			TransitionState(await currentState.Stop(waitTime));
		} finally {
			stateTransitioningActionSemaphore.Release();
		}
	}

	private interface IState {
		Task<(IState, LaunchInstanceResult)> Launch(CancellationToken cancellationToken);
		Task<bool> SendCommand(string command, CancellationToken cancellationToken);
		Task<IState> Stop(TimeSpan waitTime);
	}

	private static Task<IState> FromState(IState state) {
		return Task.FromResult(state);
	}

	private sealed class NotRunningState : IState {
		private readonly Instance instance;

		public NotRunningState(Instance instance) {
			this.instance = instance;
		}

		public async Task<(IState, LaunchInstanceResult)> Launch(CancellationToken cancellationToken) {
			var portResult = instance.usedPortTracker.MarkUsed(instance.Info);
			if (portResult == UsedPortTracker.Result.ServerPortAlreadyInUse) {
				return (this, LaunchInstanceResult.ServerPortAlreadyInUse);
			}
			else if (portResult == UsedPortTracker.Result.RconPortAlreadyInUse) {
				return (this, LaunchInstanceResult.RconPortAlreadyInUse);
			}
			
			instance.logger.Information("Session starting...");
			
			var launchResult = await instance.launcher.Launch(instance.launchServices, cancellationToken);
			if (launchResult is LaunchResult.CouldNotDownloadMinecraftServer) {
				instance.logger.Error("Session failed to launch, could not download Minecraft server.");
				return (this, LaunchInstanceResult.CouldNotDownloadMinecraftServer);
			}
			else if (launchResult is LaunchResult.InvalidJavaRuntime) {
				instance.logger.Error("Session failed to launch, invalid Java runtime.");
				return (this, LaunchInstanceResult.JavaRuntimeNotFound);
			}
			
			if (launchResult is not LaunchResult.Success launchSuccess) {
				instance.logger.Error("Session failed to launch.");
				return (this, LaunchInstanceResult.UnknownError);
			}
			
			var session = launchSuccess.Session;
			var state = new RunningState(instance, session);

			if (session.HasEnded) {
				instance.logger.Warning("Session ended immediately after it was started.");
				state.Dispose();
				return (this, LaunchInstanceResult.UnknownError);
			}
			else {
				instance.logger.Information("Session started.");
				return (state, LaunchInstanceResult.Success);
			}
		}

		public Task<bool> SendCommand(string command, CancellationToken cancellationToken) {
			return Task.FromResult(false);
		}

		public Task<IState> Stop(TimeSpan waitTime) {
			return FromState(this);
		}
	}

	private sealed class RunningState : IState, IDisposable {
		private readonly Instance instance;
		private readonly InstanceSession session;
		private readonly InstanceLogSenderThread logSenderThread;

		public RunningState(Instance instance, InstanceSession session) {
			this.instance = instance;
			this.session = session;
			this.logSenderThread = new InstanceLogSenderThread(instance.Info.InstanceGuid, instance.shortName);
			
			this.session.AddOutputListener(SessionOutput);
			this.session.SessionEnded += SessionEnded;
		}

		private void SessionOutput(object? sender, string e) {
			instance.logger.Verbose("[Server] {Line}", e);
			logSenderThread.Enqueue(e);
		}

		private void SessionEnded(object? sender, EventArgs e) {
			ReleasePorts();
			instance.logger.Information("Session ended.");
			instance.TransitionState(new NotRunningState(instance));
		}

		public Task<(IState, LaunchInstanceResult)> Launch(CancellationToken cancellationToken) {
			(IState, LaunchInstanceResult) result = (this, LaunchInstanceResult.InstanceAlreadyRunning);
			return Task.FromResult(result);
		}

		public async Task<bool> SendCommand(string command, CancellationToken cancellationToken) {
			try {
				instance.logger.Information("Sending command: {Command}", command);
				await session.SendCommand(command, cancellationToken);
				return true;
			} catch (OperationCanceledException) {
				return false;
			} catch (Exception e) {
				instance.logger.Warning(e, "Caught exception while sending command.");
				return false;
			}
		}

		public async Task<IState> Stop(TimeSpan waitTime) {
			session.SessionEnded -= SessionEnded;

			instance.logger.Information("Stopping session with a time limit of {TimeLimit}s...", waitTime.TotalSeconds);
			using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5))) {
				try {
					await session.SendCommand("stop", cts.Token);
				} catch (OperationCanceledException) {
					// ignore
				} catch (Exception e) {
					instance.logger.Warning(e, "Caught exception while sending stop command.");
				}
			}

			instance.logger.Information("Waiting for session to end...");
			using (var cts = new CancellationTokenSource(waitTime)) {
				try {
					await session.WaitForExit(cts.Token);
				} catch (OperationCanceledException) {
					try {
						instance.logger.Warning("Waiting timed out, killing session...");
						session.Kill();
					} catch (Exception e) {
						instance.logger.Warning(e, "Caught exception while killing session.");
					}
				}
			}

			ReleasePorts();
			return new NotRunningState(instance);
		}

		private void ReleasePorts() {
			instance.usedPortTracker.Release(instance.Info);
		}

		public void Dispose() {
			logSenderThread.Cancel();
			session.SessionEnded -= SessionEnded;
			session.RemoveOutputListener(SessionOutput);
			session.Dispose();
		}
	}

	public void Dispose() {
		if (currentState is IDisposable disposable) {
			disposable.Dispose();
		}

		stateTransitioningActionSemaphore.Dispose();
	}
}
