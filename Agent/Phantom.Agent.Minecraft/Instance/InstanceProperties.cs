using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Properties;

namespace Phantom.Agent.Minecraft.Instance;

public sealed record InstanceProperties(
	JavaRuntimeExecutable JavaRuntimeExecutable,
	JvmProperties JvmProperties,
	string InstanceFolder,
	string ServerVersion,
	ServerProperties ServerProperties
);
