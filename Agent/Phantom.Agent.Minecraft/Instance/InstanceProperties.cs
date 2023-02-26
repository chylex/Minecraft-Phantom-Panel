using System.Collections.Immutable;
using Phantom.Agent.Minecraft.Java;
using Phantom.Agent.Minecraft.Properties;
using Phantom.Common.Data.Instance;

namespace Phantom.Agent.Minecraft.Instance;

public sealed record InstanceProperties(
	Guid InstanceGuid,
	Guid JavaRuntimeGuid,
	JvmProperties JvmProperties,
	ImmutableArray<string> JvmArguments,
	string InstanceFolder,
	string ServerVersion,
	ServerProperties ServerProperties,
	InstanceLaunchProperties LaunchProperties
);
