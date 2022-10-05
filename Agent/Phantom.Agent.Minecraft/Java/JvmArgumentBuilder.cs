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

		// TODO
		target.Add("-Xshare:off");
		target.Add("-XX:+UnlockExperimentalVMOptions");
		target.Add("-XX:+UseZGC");
		target.Add("-XX:+ZProactive");
		target.Add("-XX:ZCollectionInterval=600");
		target.Add("-XX:+DisableExplicitGC");
		target.Add("-XX:+AlwaysPreTouch");
		target.Add("-XX:+ParallelRefProcEnabled");
		target.Add("-XX:+PerfDisableSharedMem");

		foreach (var property in customProperties) {
			target.Add(property);
		}
	}
}
