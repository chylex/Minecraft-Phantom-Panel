using Phantom.Common.Data.Agent.Instance;
using Phantom.Common.Data.Web.Instance;

namespace Phantom.Controller.Services.Instances;

static class InstanceExtensions {
	extension(InstanceConfiguration configuration) {
		public InstanceInfo AsInfo => new (configuration.InstanceName, configuration.ServerPort, [configuration.RconPort], configuration.MemoryAllocation);
	}
}
