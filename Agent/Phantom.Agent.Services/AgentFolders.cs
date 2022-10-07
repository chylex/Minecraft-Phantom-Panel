using Phantom.Common.Logging;
using Phantom.Utils.IO;
using Serilog;

namespace Phantom.Agent.Services;

public sealed class AgentFolders {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentFolders>();

	public string DataFolderPath { get; }
	public string TemporaryFolderPath { get; }
	public string JavaSearchFolderPath { get; }

	public AgentFolders(string dataFolderPath, string temporaryFolderPath, string javaSearchFolderPath) {
		this.DataFolderPath = Path.GetFullPath(dataFolderPath);
		this.TemporaryFolderPath = Path.GetFullPath(temporaryFolderPath);
		this.JavaSearchFolderPath = javaSearchFolderPath;
	}

	public bool TryCreate() {
		return TryCreateFolder(DataFolderPath) &&
		       TryCreateFolder(TemporaryFolderPath);
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
