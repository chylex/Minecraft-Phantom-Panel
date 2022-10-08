using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;

namespace Phantom.Agent.Services.Instances.States; 

interface IInstanceState {
	void Initialize();
	(IInstanceState, LaunchInstanceResult) Launch(InstanceContext context);
	(IInstanceState, StopInstanceResult) Stop(MinecraftStopStrategy stopStrategy);
	Task<bool> SendCommand(string command, CancellationToken cancellationToken);
}
