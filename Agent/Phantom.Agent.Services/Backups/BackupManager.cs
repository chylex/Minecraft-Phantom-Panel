using Phantom.Agent.Services.Instances;

namespace Phantom.Agent.Services.Backups; 

sealed class BackupManager {
	private readonly string basePath;

	public BackupManager(AgentFolders agentFolders) {
		this.basePath = agentFolders.BackupsFolderPath;
	}

	public async Task CreateBackup(Instance instance) {
		
	}
}
