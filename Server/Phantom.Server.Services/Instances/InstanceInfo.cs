using Phantom.Common.Data;

namespace Phantom.Server.Services.Instances; 

public sealed record InstanceInfo(
	Guid AgentGuid,
	Guid InstanceGuid,
	string InstanceName,
	string MinecraftVersion,
	MinecraftServerKind MinecraftServerKind
);
