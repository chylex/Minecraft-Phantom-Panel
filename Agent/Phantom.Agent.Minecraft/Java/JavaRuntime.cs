using Phantom.Common.Data.Java;

namespace Phantom.Agent.Minecraft.Java; 

public sealed class JavaRuntime {
	internal string ExecutablePath { get; }
	public JavaVersion Version { get; }

	public JavaRuntime(string executablePath, JavaVersion version) {
		this.ExecutablePath = executablePath;
		this.Version = version;
	}
}
