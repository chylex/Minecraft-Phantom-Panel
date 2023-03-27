using Phantom.Agent.Services.Backups;
using Phantom.Agent.Services.Instances.States;
using Phantom.Common.Data.Backups;

namespace Phantom.Agent.Services.Instances.Procedures;

sealed record BackupInstanceProcedure(BackupManager BackupManager) : IInstanceProcedure {
	private readonly TaskCompletionSource<BackupCreationResult> resultCompletionSource = new ();

	public Task<BackupCreationResult> Result => resultCompletionSource.Task;

	public async Task<IInstanceState?> Run(IInstanceContext context, CancellationToken cancellationToken) {
		if (context.CurrentState is not InstanceRunningState runningState || runningState.Process.HasEnded) {
			resultCompletionSource.SetResult(new BackupCreationResult(BackupCreationResultKind.InstanceNotRunning));
			return null;
		}

		try {
			var result = await BackupManager.CreateBackup(context.ShortName, runningState.Process, cancellationToken);
			resultCompletionSource.SetResult(result);
		} catch (OperationCanceledException) {
			resultCompletionSource.SetCanceled(cancellationToken);
		} catch (Exception e) {
			resultCompletionSource.SetException(e);
		}

		return null;
	}
}
