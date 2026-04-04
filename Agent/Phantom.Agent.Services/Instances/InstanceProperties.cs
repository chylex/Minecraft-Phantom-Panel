namespace Phantom.Agent.Services.Instances;

sealed record InstanceProperties(
	Guid InstanceGuid,
	string InstanceDirectoryPath
);
