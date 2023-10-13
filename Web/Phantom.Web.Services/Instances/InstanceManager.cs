using System.Collections.Immutable;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Data.Web.Instance;
using Phantom.Common.Logging;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Utils.Events;
using Phantom.Web.Services.Rpc;

namespace Phantom.Web.Services.Instances;

using InstanceDictionary = ImmutableDictionary<Guid, Instance>;

public sealed class InstanceManager {
	private readonly ControllerConnection controllerConnection;
	private readonly SimpleObservableState<InstanceDictionary> instances = new (PhantomLogger.Create<InstanceManager>("Instances"), InstanceDictionary.Empty);
	
	public InstanceManager(ControllerConnection controllerConnection) {
		this.controllerConnection = controllerConnection;
	}

	public EventSubscribers<InstanceDictionary> InstancesChanged => instances.Subs;
	
	internal void RefreshInstances(ImmutableArray<Instance> newInstances) {
		instances.SetTo(newInstances.ToImmutableDictionary(static instance => instance.Configuration.InstanceGuid));
	}

	public InstanceDictionary GetAll() {
		return instances.Value;
	}

	public Instance? GetByGuid(Guid instanceGuid) {
		return instances.Value.GetValueOrDefault(instanceGuid);
	}

	public Task<InstanceActionResult<CreateOrUpdateInstanceResult>> CreateOrUpdateInstance(Guid loggedInUserGuid, InstanceConfiguration configuration, CancellationToken cancellationToken) {
		var message = new CreateOrUpdateInstanceMessage(loggedInUserGuid, configuration);
		return controllerConnection.Send<CreateOrUpdateInstanceMessage, InstanceActionResult<CreateOrUpdateInstanceResult>>(message, cancellationToken);
	}

	public Task<InstanceActionResult<LaunchInstanceResult>> LaunchInstance(Guid loggedInUserGuid, Guid instanceGuid, CancellationToken cancellationToken) {
		var message = new LaunchInstanceMessage(loggedInUserGuid, instanceGuid);
		return controllerConnection.Send<LaunchInstanceMessage, InstanceActionResult<LaunchInstanceResult>>(message, cancellationToken);
	}

	public Task<InstanceActionResult<StopInstanceResult>> StopInstance(Guid loggedInUserGuid, Guid instanceGuid, MinecraftStopStrategy stopStrategy, CancellationToken cancellationToken) {
		var message = new StopInstanceMessage(loggedInUserGuid, instanceGuid, stopStrategy);
		return controllerConnection.Send<StopInstanceMessage, InstanceActionResult<StopInstanceResult>>(message, cancellationToken);
	}

	public Task<InstanceActionResult<SendCommandToInstanceResult>> SendCommandToInstance(Guid loggedInUserGuid, Guid instanceGuid, string command, CancellationToken cancellationToken) {
		var message = new SendCommandToInstanceMessage(loggedInUserGuid, instanceGuid, command);
		return controllerConnection.Send<SendCommandToInstanceMessage, InstanceActionResult<SendCommandToInstanceResult>>(message, cancellationToken);
	}
}
