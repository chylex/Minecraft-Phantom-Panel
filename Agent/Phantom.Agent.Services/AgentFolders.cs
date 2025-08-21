using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Services;

public sealed class AgentFolders {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentFolders>();
	
	public string DataFolderPath { get; }
	public string InstancesFolderPath { get; }
	public string BackupsFolderPath { get; }
	
	public string TemporaryFolderPath { get; }
	public string ServerExecutableFolderPath { get; }
	
	public string JavaSearchFolderPath { get; }
	
	public AgentFolders(string dataFolderPath, string temporaryFolderPath, string javaSearchFolderPath) {
		this.DataFolderPath = Path.GetFullPath(dataFolderPath);
		this.InstancesFolderPath = Path.Combine(DataFolderPath, "instances");
		this.BackupsFolderPath = Path.Combine(DataFolderPath, "backups");
		
		this.TemporaryFolderPath = Path.GetFullPath(temporaryFolderPath);
		this.ServerExecutableFolderPath = Path.Combine(TemporaryFolderPath, "servers");
		
		this.JavaSearchFolderPath = javaSearchFolderPath;
	}
	
	public bool TryCreate() {
		return TryCreateFolder(DataFolderPath) &&
		       TryCreateFolder(InstancesFolderPath) &&
		       TryCreateFolder(BackupsFolderPath) &&
		       TryCreateFolder(TemporaryFolderPath) &&
		       TryCreateFolder(ServerExecutableFolderPath);
	}
	
	private static bool TryCreateFolder(string folderPath) {
		if (Directory.Exists(folderPath)) {
			return true;
		}
		
		try {
			Directories.Create(folderPath, Chmod.URWX_GRX);
			return true;
		} catch (Exception e) {
			Logger.Fatal(e, "Error creating folder: {FolderPath}", folderPath);
			return false;
		}
	}
}
