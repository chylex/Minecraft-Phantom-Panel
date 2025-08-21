using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Phantom.Agent.Minecraft.Command;
using Phantom.Agent.Minecraft.Instance;
using Phantom.Utils.Tasks;
using Serilog;

namespace Phantom.Agent.Services.Backups;

sealed partial class BackupServerCommandDispatcher : IDisposable {
	[GeneratedRegex(@"^(?:(?:\[.*?\] \[Server thread/INFO\].*?:)|(?:[\d-]+? [\d:]+? \[INFO\])) (.*?)$", RegexOptions.NonBacktracking)]
	private static partial Regex ServerThreadInfoRegex();
	
	private static readonly ImmutableHashSet<string> AutomaticSavingDisabledMessages = ImmutableHashSet.Create(
		"Automatic saving is now disabled",
		"Turned off world auto-saving",
		"CONSOLE: Disabling level saving.."
	);
	
	private static readonly ImmutableHashSet<string> SavedTheGameMessages = ImmutableHashSet.Create(
		"Saved the game",
		"Saved the world",
		"CONSOLE: Save complete."
	);
	
	private static readonly ImmutableHashSet<string> AutomaticSavingEnabledMessages = ImmutableHashSet.Create(
		"Automatic saving is now enabled",
		"Turned on world auto-saving",
		"CONSOLE: Enabling level saving.."
	);
	
	private readonly ILogger logger;
	private readonly InstanceProcess process;
	private readonly CancellationToken cancellationToken;
	
	private readonly TaskCompletionSource automaticSavingDisabled = AsyncTasks.CreateCompletionSource();
	private readonly TaskCompletionSource savedTheGame = AsyncTasks.CreateCompletionSource();
	private readonly TaskCompletionSource automaticSavingEnabled = AsyncTasks.CreateCompletionSource();
	
	public BackupServerCommandDispatcher(ILogger logger, InstanceProcess process, CancellationToken cancellationToken) {
		this.logger = logger;
		this.process = process;
		this.cancellationToken = cancellationToken;
		
		this.process.AddOutputListener(OnOutput, maxLinesToReadFromHistory: 0);
	}
	
	void IDisposable.Dispose() {
		process.RemoveOutputListener(OnOutput);
	}
	
	public async Task DisableAutomaticSaving() {
		await process.SendCommand(MinecraftCommand.SaveOff, cancellationToken);
		await automaticSavingDisabled.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
	}
	
	public async Task SaveAllChunks() {
		await process.SendCommand(MinecraftCommand.SaveAll(flush: true), cancellationToken);
		await savedTheGame.Task.WaitAsync(TimeSpan.FromMinutes(1), cancellationToken);
	}
	
	public async Task EnableAutomaticSaving() {
		await process.SendCommand(MinecraftCommand.SaveOn, cancellationToken);
		await automaticSavingEnabled.Task.WaitAsync(TimeSpan.FromMinutes(1), cancellationToken);
	}
	
	private void OnOutput(object? sender, string? line) {
		if (line == null) {
			return;
		}
		
		var match = ServerThreadInfoRegex().Match(line);
		if (!match.Success) {
			return;
		}
		
		string info = match.Groups[1].Value;
		
		if (!automaticSavingDisabled.Task.IsCompleted) {
			if (AutomaticSavingDisabledMessages.Contains(info)) {
				logger.Debug("Detected that automatic saving is disabled.");
				automaticSavingDisabled.SetResult();
			}
		}
		else if (!savedTheGame.Task.IsCompleted) {
			if (SavedTheGameMessages.Contains(info)) {
				logger.Debug("Detected that the game is saved.");
				savedTheGame.SetResult();
			}
		}
		else if (!automaticSavingEnabled.Task.IsCompleted) {
			if (AutomaticSavingEnabledMessages.Contains(info)) {
				logger.Debug("Detected that automatic saving is enabled.");
				automaticSavingEnabled.SetResult();
			}
		}
	}
}
