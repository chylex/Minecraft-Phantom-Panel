using Phantom.Common.Data.Java;

namespace Phantom.Agent.Minecraft.Java;

public sealed class JavaRuntimeExecutable {
	internal string ExecutablePath { get; }
	internal JavaRuntime Runtime { get; }
	
	internal JavaRuntimeExecutable(string executablePath, JavaRuntime runtime) {
		this.ExecutablePath = executablePath;
		this.Runtime = runtime;
	}
}
