using Phantom.Common.Data.Instance;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Actor;
using Serilog;

namespace Phantom.Agent.Services.Instances;

sealed record InstanceContext(Guid InstanceGuid, string ShortName, ILogger Logger, InstanceServices Services, ActorRef<InstanceActor.ICommand> Actor, CancellationToken ActorCancellationToken) {
	public void ReportEvent(IInstanceEvent instanceEvent) {
		Services.ControllerConnection.Send(new ReportInstanceEventMessage(Guid.NewGuid(), DateTime.UtcNow, InstanceGuid, instanceEvent));
	}
}
