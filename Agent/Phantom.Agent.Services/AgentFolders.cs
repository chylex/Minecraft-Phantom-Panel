using Phantom.Common.Logging;
using Phantom.Utils.IO;
using Serilog;

namespace Phantom.Agent.Services;

public sealed class AgentFolders {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentFolders>();

	public string DataFolderPath { get; }
	public string InstancesFolderPath { get; }

	public AgentFolders(string dataFolder) {
		this.DataFolderPath = Path.GetFullPath(dataFolder);
		this.InstancesFolderPath = Path.Combine(DataFolderPath, "instances");
	}

	public bool TryCreate() {
		return TryCreateFolder(DataFolderPath) &&
		       TryCreateFolder(InstancesFolderPath);
	}

	private static bool TryCreateFolder(string folderPath) {
		if (Directory.Exists(folderPath)) {
			return true;
		}

		try {
			Directories.Create(folderPath, Chmod.URW_GR);
			return true;
		} catch (Exception e) {
			Logger.Fatal(e, "Error creating folder: {FolderPath}", folderPath);
			return false;
		}
	}
}
