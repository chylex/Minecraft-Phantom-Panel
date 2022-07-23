namespace Phantom.Agent.Minecraft.Java; 

public sealed class JavaRuntime {
	private readonly string javaHome;
	
	public JavaRuntime(string javaHome) {
		this.javaHome = javaHome;
	}
	
	public string JavaExecutablePath => javaHome + "/bin/java";
}
