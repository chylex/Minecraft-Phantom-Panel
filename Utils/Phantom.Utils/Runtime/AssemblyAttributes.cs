using System.Reflection;

namespace Phantom.Utils.Runtime;

public static class AssemblyAttributes {
	public static string GetFullVersion(Assembly assembly) {
		return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Replace('+', '/') ?? string.Empty;
	}
}
