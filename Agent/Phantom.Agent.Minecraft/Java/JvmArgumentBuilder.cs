using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Phantom.Agent.Minecraft.Java;

sealed class JvmArgumentBuilder {
	private readonly JvmProperties basicProperties;
	private readonly List<string> customArguments = new ();

	public JvmArgumentBuilder(JvmProperties basicProperties, ImmutableArray<string> customArguments) {
		this.basicProperties = basicProperties;

		foreach (var jvmArgument in customArguments) {
			this.customArguments.Add(jvmArgument);
		}
	}

	public void AddProperty(string key, string value) {
		customArguments.Add("-D" + key + "=\"" + value + "\""); // TODO test quoting?
	}

	public void Build(Collection<string> target) {
		foreach (var property in customArguments) {
			target.Add(property);
		}

		target.Add("-Xms" + basicProperties.InitialHeapMegabytes + "M");
		target.Add("-Xmx" + basicProperties.MaximumHeapMegabytes + "M");
		target.Add("-Xrs");
	}
}
