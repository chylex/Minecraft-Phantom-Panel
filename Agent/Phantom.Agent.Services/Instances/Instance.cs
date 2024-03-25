using Phantom.Common.Data.Instance;

namespace Phantom.Agent.Services.Instances;

sealed record Instance(Guid InstanceGuid, IInstanceStatus Status);
