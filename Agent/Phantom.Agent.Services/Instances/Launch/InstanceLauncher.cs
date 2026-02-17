using System.Collections.Immutable;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Services.Downloads;
using Phantom.Agent.Services.Instances.State;
using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Agent.Instance;
using Phantom.Common.Data.Agent.Instance.Launch;
using Phantom.Common.Data.Instance;
using Phantom.Utils.Processes;
using Serilog;

namespace Phantom.Agent.Services.Instances.Launch;

sealed class InstanceLauncher(
	FileDownloadManager downloadManager,
	IInstancePathResolver pathResolver,
	IInstanceValueResolver valueResolver,
	InstanceProperties instanceProperties,
	InstanceLaunchRecipe launchRecipe
) {
	public async Task<InstanceLaunchResult> Launch(ILogger logger, Action<IInstanceStatus?> reportStatus, CancellationToken cancellationToken) {
		string? executablePath = launchRecipe.Executable.Resolve(pathResolver);
		if (executablePath == null) {
			logger.Error("Could not resolve server executable path: {Path}", launchRecipe.Executable);
			return new InstanceLaunchResult.CouldNotFindServerExecutable();
		}
		
		var stepExecutor = new StepExecutor(logger, downloadManager, pathResolver, reportStatus, cancellationToken);
		var steps = launchRecipe.Preparation;
		
		for (int stepIndex = 0; stepIndex < steps.Length; stepIndex++) {
			var step = steps[stepIndex];
			try {
				if (await step.Run(stepExecutor)) {
					continue;
				}
			} catch (Exception e) {
				logger.Error(e, "Failed preparation step {StepIndex} out of {StepCount}: {StepName}", stepIndex, steps.Length, step.GetType().Name);
			}
			
			return new InstanceLaunchResult.CouldNotPrepareServerInstance();
		}
		
		var processConfigurator = new ProcessConfigurator {
			FileName = executablePath,
			WorkingDirectory = instanceProperties.InstanceFolder,
			RedirectInput = true,
			UseShellExecute = false,
		};
		
		var processArguments = processConfigurator.ArgumentList;
		
		foreach (IInstanceValue value in launchRecipe.Arguments) {
			if (value.Resolve(valueResolver) is {} resolved) {
				processArguments.Add(resolved);
			}
			else {
				logger.Error("Could not resolve server executable argument: {Value}", value);
				return new InstanceLaunchResult.CouldNotPrepareServerInstance();
			}
		}
		
		var process = processConfigurator.CreateProcess();
		var instanceProcess = new InstanceProcess(instanceProperties, process);
		
		try {
			process.Start();
		} catch (Exception launchException) {
			logger.Error(launchException, "Caught exception launching the server process.");
			
			try {
				process.Kill();
			} catch (Exception killException) {
				logger.Error(killException, "Caught exception trying to kill the server process after a failed launch.");
			}
			
			return new InstanceLaunchResult.CouldNotStartServerExecutable();
		}
		
		return new InstanceLaunchResult.Success(instanceProcess);
	}
	
	private sealed class StepExecutor(ILogger logger, FileDownloadManager downloadManager, IInstancePathResolver pathResolver, Action<IInstanceStatus?> reportStatus, CancellationToken cancellationToken) : IInstanceLaunchStepExecutor<bool> {
		public async Task<bool> DownloadFile(FileDownloadInfo downloadInfo, IInstancePath path) {
			string? filePath = path.Resolve(pathResolver);
			if (filePath == null) {
				logger.Error("Could not resolve download file path: {Path}", path);
				return false;
			}
			
			byte? lastDownloadProgress = null;
			
			void OnDownloadProgress(object? sender, DownloadProgressEventArgs args) {
				byte? progress = args.TotalBytes is not {} totalBytes ? null : (byte) Math.Min(args.DownloadedBytes * 100 / totalBytes, val2: 100);
				
				if (lastDownloadProgress != progress) {
					lastDownloadProgress = progress;
					reportStatus(InstanceStatus.Downloading(progress));
				}
			}
			
			if (await downloadManager.DownloadAndGetPath(downloadInfo, filePath, OnDownloadProgress, cancellationToken) == null) {
				logger.Error("Could not download file: {Url}", downloadInfo.Url);
				return false;
			}
			
			reportStatus(null);
			return true;
		}
		
		public async Task<bool> EditPropertiesFile(InstancePath.Local path, string comment, ImmutableDictionary<string, string> newValues) {
			string? filePath = path.Resolve(pathResolver);
			if (filePath == null) {
				logger.Error("Could not resolve properties file path: {Path}", path);
				return false;
			}
			
			var editor = new JavaPropertiesFileEditor();
			editor.SetAll(newValues);
			
			try {
				await editor.EditOrCreate(filePath, comment, cancellationToken);
			} catch (Exception e) {
				logger.Error(e, "Could not edit properties file: {Path}", filePath);
				return false;
			}
			
			return true;
		}
	}
}
