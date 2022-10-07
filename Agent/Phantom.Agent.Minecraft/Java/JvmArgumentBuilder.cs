using System.Collections.ObjectModel;

namespace Phantom.Agent.Minecraft.Java;

sealed class JvmArgumentBuilder {
	private readonly JvmProperties basicProperties;
	private readonly List<string> customProperties = new ();

	public JvmArgumentBuilder(JvmProperties basicProperties) {
		this.basicProperties = basicProperties;
	}

	public void AddProperty(string key, string value) {
		customProperties.Add("-D" + key + "=\"" + value + "\""); // TODO test quoting?
	}

	public void Build(Collection<string> target) {
		target.Add("-Xms" + basicProperties.InitialHeapMegabytes + "M");
		target.Add("-Xmx" + basicProperties.MaximumHeapMegabytes + "M");

		foreach (var property in customProperties) {
			target.Add(property);
		}
	}
}
