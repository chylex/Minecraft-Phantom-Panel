using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Properties;

namespace Phantom.Agent.Minecraft.Instance;

public sealed record InstanceProperties(
	Guid JavaRuntimeGuid,
	JvmProperties JvmProperties,
	string InstanceFolder,
	string ServerVersion,
	ServerProperties ServerProperties
);
