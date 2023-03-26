using Phantom.Agent.Services.Instances.States;

namespace Phantom.Agent.Services.Instances.Procedures; 

interface IInstanceProcedure {
	Task<IInstanceState?> Run(IInstanceContext context, CancellationToken cancellationToken);
}
