using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Phantom.Utils.Processes;

public sealed class ProcessConfigurator {
	private readonly ProcessStartInfo startInfo = new () {
		RedirectStandardOutput = true,
		RedirectStandardError = true,
	};
	
	public string FileName {
		get => startInfo.FileName;
		set => startInfo.FileName = value;
	}
	
	public Collection<string> ArgumentList => startInfo.ArgumentList;
	
	public string WorkingDirectory {
		get => startInfo.WorkingDirectory;
		set => startInfo.WorkingDirectory = value;
	}
	
	public bool RedirectInput {
		get => startInfo.RedirectStandardInput;
		set => startInfo.RedirectStandardInput = value;
	}
	
	public bool UseShellExecute {
		get => startInfo.UseShellExecute;
		set => startInfo.UseShellExecute = value;
	}
	
	public ProcessConfigurator() {
		if (OperatingSystem.IsWindows()) {
			startInfo.CreateNewProcessGroup = true;
		}
	}
	
	public Process CreateProcess() {
		return new Process(new System.Diagnostics.Process { StartInfo = startInfo });
	}
}
