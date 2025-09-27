using System.Collections.Immutable;
using Phantom.Common.Data.Web.Agent;
using Phantom.Common.Data.Web.Instance;
using Phantom.Common.Messages.Web;
using Phantom.Common.Messages.Web.ToWeb;
using Phantom.Controller.Services.Instances;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Controller.Services.Rpc;

sealed class WebMessageDataUpdateSenderActor : ReceiveActor<WebMessageDataUpdateSenderActor.ICommand> {
	public readonly record struct Init(MessageSender<IMessageToWeb> MessageSender, ControllerState ControllerState, InstanceLogManager InstanceLogManager);
	
	public static Props<ICommand> Factory(Init init) {
		return Props<ICommand>.Create(() => new WebMessageDataUpdateSenderActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume });
	}
	
	private readonly MessageSender<IMessageToWeb> messageSender;
	private readonly ControllerState controllerState;
	private readonly InstanceLogManager instanceLogManager;
	private readonly ActorRef<ICommand> selfCached;
	
	private WebMessageDataUpdateSenderActor(Init init) {
		this.messageSender = init.MessageSender;
		this.controllerState = init.ControllerState;
		this.instanceLogManager = init.InstanceLogManager;
		this.selfCached = SelfTyped;
		
		ReceiveAsync<RefreshAgentsCommand>(RefreshAgents);
		ReceiveAsync<RefreshInstancesCommand>(RefreshInstances);
		ReceiveAsync<ReceiveInstanceLogsCommand>(ReceiveInstanceLogs);
		ReceiveAsync<RefreshUserSessionCommand>(RefreshUserSession);
	}
	
	protected override void PreStart() {
		controllerState.AgentsByGuidReceiver.Register(SelfTyped, static state => new RefreshAgentsCommand(state));
		controllerState.InstancesByGuidReceiver.Register(SelfTyped, static state => new RefreshInstancesCommand(state));
		
		controllerState.UserUpdatedOrDeleted += OnUserUpdatedOrDeleted;
		
		instanceLogManager.LogsReceived += OnInstanceLogsReceived;
	}
	
	protected override void PostStop() {
		instanceLogManager.LogsReceived -= OnInstanceLogsReceived;
		
		controllerState.UserUpdatedOrDeleted -= OnUserUpdatedOrDeleted;
		
		controllerState.AgentsByGuidReceiver.Unregister(SelfTyped);
		controllerState.InstancesByGuidReceiver.Unregister(SelfTyped);
	}
	
	private void OnUserUpdatedOrDeleted(object? sender, Guid userGuid) {
		selfCached.Tell(new RefreshUserSessionCommand(userGuid));
	}
	
	private void OnInstanceLogsReceived(object? sender, InstanceLogManager.Event e) {
		selfCached.Tell(new ReceiveInstanceLogsCommand(e.InstanceGuid, e.Lines));
	}
	
	public interface ICommand;
	
	private sealed record RefreshAgentsCommand(ImmutableDictionary<Guid, Agent> Agents) : ICommand;
	
	private sealed record RefreshInstancesCommand(ImmutableDictionary<Guid, Instance> Instances) : ICommand;
	
	private sealed record ReceiveInstanceLogsCommand(Guid InstanceGuid, ImmutableArray<string> Lines) : ICommand;
	
	private sealed record RefreshUserSessionCommand(Guid UserGuid) : ICommand;
	
	private Task RefreshAgents(RefreshAgentsCommand command) {
		return messageSender.Send(new RefreshAgentsMessage([..command.Agents.Values])).AsTask();
	}
	
	private Task RefreshInstances(RefreshInstancesCommand command) {
		return messageSender.Send(new RefreshInstancesMessage([..command.Instances.Values])).AsTask();
	}
	
	private Task ReceiveInstanceLogs(ReceiveInstanceLogsCommand command) {
		return messageSender.Send(new InstanceOutputMessage(command.InstanceGuid, command.Lines)).AsTask();
	}
	
	private Task RefreshUserSession(RefreshUserSessionCommand command) {
		return messageSender.Send(new RefreshUserSessionMessage(command.UserGuid)).AsTask();
	}
}
