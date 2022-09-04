using Phantom.Common.Data.Java;

namespace Phantom.Agent.Minecraft.Java; 

public sealed class JavaRuntimeExecutable {
	public string ExecutablePath { get; }
	public JavaRuntime Runtime { get; }

	internal JavaRuntimeExecutable(string executablePath, JavaRuntime runtime) {
		this.ExecutablePath = executablePath;
		this.Runtime = runtime;
	}
}
