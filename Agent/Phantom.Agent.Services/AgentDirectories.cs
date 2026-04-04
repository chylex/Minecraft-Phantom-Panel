using Phantom.Utils.IO;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Services;

public sealed class AgentDirectories {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentDirectories>();
	
	internal string DataDirectoryPath { get; }
	internal string InstancesDirectoryPath { get; }
	internal string BackupsDirectoryPath { get; }
	
	internal string TemporaryDirectoryPath { get; }
	internal string ServerExecutableDirectoryPath { get; }
	
	public string JavaSearchDirectoryPath { get; }
	
	public AgentDirectories(string dataDirectoryPath, string temporaryDirectoryPath, string javaSearchDirectoryPath) {
		this.DataDirectoryPath = Path.GetFullPath(dataDirectoryPath);
		this.InstancesDirectoryPath = Path.Combine(DataDirectoryPath, "instances");
		this.BackupsDirectoryPath = Path.Combine(DataDirectoryPath, "backups");
		
		this.TemporaryDirectoryPath = Path.GetFullPath(temporaryDirectoryPath);
		this.ServerExecutableDirectoryPath = Path.Combine(TemporaryDirectoryPath, "servers");
		
		this.JavaSearchDirectoryPath = javaSearchDirectoryPath;
	}
	
	public bool TryCreate() {
		return TryCreateDirectory(DataDirectoryPath) &&
		       TryCreateDirectory(InstancesDirectoryPath) &&
		       TryCreateDirectory(BackupsDirectoryPath) &&
		       TryCreateDirectory(TemporaryDirectoryPath) &&
		       TryCreateDirectory(ServerExecutableDirectoryPath);
	}
	
	private static bool TryCreateDirectory(string directoryPath) {
		if (Directory.Exists(directoryPath)) {
			return true;
		}
		
		try {
			Directories.Create(directoryPath, Chmod.URWX_GRX);
			return true;
		} catch (Exception e) {
			Logger.Fatal(e, "Error creating directory: {DirectoryPath}", directoryPath);
			return false;
		}
	}
}
