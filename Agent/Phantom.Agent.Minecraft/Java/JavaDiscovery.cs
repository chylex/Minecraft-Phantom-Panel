namespace Phantom.Agent.Minecraft.Java; 

public static class JavaDiscovery {
	public static string? GetSystemJavaSearchPath() {
		const string LinuxJavaPath = "/usr/lib/jvm";
		
		if (OperatingSystem.IsLinux() && Directory.Exists(LinuxJavaPath)) {
			return LinuxJavaPath;
		}
		
		return null;
	}
}
