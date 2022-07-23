using System.Collections.ObjectModel;

namespace Phantom.Agent.Minecraft.Java; 

sealed class JvmArgumentBuilder {
	public uint? InitialHeapMegabytes { get; init; }
	public uint? MaximumHeapMegabytes { get; init; }
	
	private readonly List<string> properties = new ();

	public void AddProperty(string key, string value) {
		properties.Add("-D" + key + "=\"" + value + "\""); // TODO test quoting?
	}
	
	public void Build(Collection<string> target) {
		if (InitialHeapMegabytes != null) {
			target.Add("-Xms" + InitialHeapMegabytes + "M");
		}

		if (MaximumHeapMegabytes != null) {
			target.Add("-Xmx" + MaximumHeapMegabytes + "M");
		}
		
		foreach (var property in properties) {
			target.Add(property);
		}
	}
}
