using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Properties;

namespace Phantom.Agent.Minecraft.Instance;

public sealed record InstanceProperties(
	JavaRuntime JavaRuntime,
	JvmProperties JvmProperties,
	string InstanceFolder,
	string ServerJarPath,
	ServerProperties ServerProperties
);
