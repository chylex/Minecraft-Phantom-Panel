using System.Collections.Immutable;
using Phantom.Common.Data;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Replies;
using Phantom.Common.Data.Web.Instance;
using Phantom.Common.Data.Web.Users;
using Phantom.Common.Messages.Web.ToController;
using Phantom.Utils.Events;
using Phantom.Utils.Logging;
using Phantom.Web.Services.Authentication;
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
		instances.SetTo(newInstances.ToImmutableDictionary(static instance => instance.InstanceGuid));
	}

	public InstanceDictionary GetAll() {
		return instances.Value;
	}

	public Instance? GetByGuid(AuthenticatedUser? authenticatedUser, Guid instanceGuid) {
		if (authenticatedUser == null) {
			return null;
		}
		
		var instance = instances.Value.GetValueOrDefault(instanceGuid);
		return instance != null && authenticatedUser.Info.HasAccessToAgent(instance.Configuration.AgentGuid) ? instance : null;
	}

	public async Task<Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>> CreateOrUpdateInstance(AuthenticatedUser? authenticatedUser, Guid instanceGuid, InstanceConfiguration configuration, CancellationToken cancellationToken) {
		if (authenticatedUser != null && authenticatedUser.Info.CheckPermission(Permission.CreateInstances)) {
			var message = new CreateOrUpdateInstanceMessage(authenticatedUser.Token, instanceGuid, configuration);
			return await controllerConnection.Send<CreateOrUpdateInstanceMessage, Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>>(message, cancellationToken);
		}
		else {
			return (UserInstanceActionFailure) UserActionFailure.NotAuthorized;
		}
	}

	public async Task<Result<LaunchInstanceResult, UserInstanceActionFailure>> LaunchInstance(AuthenticatedUser? authenticatedUser, Guid agentGuid, Guid instanceGuid, CancellationToken cancellationToken) {
		if (authenticatedUser != null && authenticatedUser.Info.CheckPermission(Permission.ControlInstances)) {
			var message = new LaunchInstanceMessage(authenticatedUser.Token, agentGuid, instanceGuid);
			return await controllerConnection.Send<LaunchInstanceMessage, Result<LaunchInstanceResult, UserInstanceActionFailure>>(message, cancellationToken);
		}
		else {
			return (UserInstanceActionFailure) UserActionFailure.NotAuthorized;
		}
	}

	public async Task<Result<StopInstanceResult, UserInstanceActionFailure>> StopInstance(AuthenticatedUser? authenticatedUser, Guid agentGuid, Guid instanceGuid, MinecraftStopStrategy stopStrategy, CancellationToken cancellationToken) {
		if (authenticatedUser != null && authenticatedUser.Info.CheckPermission(Permission.ControlInstances)) {
			var message = new StopInstanceMessage(authenticatedUser.Token, agentGuid, instanceGuid, stopStrategy);
			return await controllerConnection.Send<StopInstanceMessage, Result<StopInstanceResult, UserInstanceActionFailure>>(message, cancellationToken);
		}
		else {
			return (UserInstanceActionFailure) UserActionFailure.NotAuthorized;
		}
	}

	public async Task<Result<SendCommandToInstanceResult, UserInstanceActionFailure>> SendCommandToInstance(AuthenticatedUser? authenticatedUser, Guid agentGuid, Guid instanceGuid, string command, CancellationToken cancellationToken) {
		if (authenticatedUser != null && authenticatedUser.Info.CheckPermission(Permission.ControlInstances)) {
			var message = new SendCommandToInstanceMessage(authenticatedUser.Token, agentGuid, instanceGuid, command);
			return await controllerConnection.Send<SendCommandToInstanceMessage, Result<SendCommandToInstanceResult, UserInstanceActionFailure>>(message, cancellationToken);
		}
		else {
			return (UserInstanceActionFailure) UserActionFailure.NotAuthorized;
		}
	}
}
