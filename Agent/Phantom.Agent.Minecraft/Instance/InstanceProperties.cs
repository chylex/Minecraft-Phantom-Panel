using System.Collections.Immutable;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Properties;

namespace Phantom.Agent.Minecraft.Instance;

public sealed record InstanceProperties(
	Guid JavaRuntimeGuid,
	JvmProperties JvmProperties,
	ImmutableArray<string> JvmArguments,
	string InstanceFolder,
	string ServerVersion,
	ServerProperties ServerProperties
);
