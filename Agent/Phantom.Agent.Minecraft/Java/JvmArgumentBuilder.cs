using System.Collections.ObjectModel;

namespace Phantom.Agent.Minecraft.Java;

sealed class JvmArgumentBuilder(JvmProperties basicProperties) {
	private readonly List<string> customArguments = [];
	
	public void Add(string argument) {
		customArguments.Add(argument);
	}
	
	public void AddProperty(string key, string value) {
		customArguments.Add("-D" + key + "=\"" + value + "\""); // TODO test quoting?
	}
	
	public void Build(Collection<string> target) {
		foreach (var property in customArguments) {
			target.Add(property);
		}
		
		// In case of duplicate JVM arguments, typically the last one wins.
		target.Add("-Xms" + basicProperties.InitialHeapMegabytes + "M");
		target.Add("-Xmx" + basicProperties.MaximumHeapMegabytes + "M");
	}
}
