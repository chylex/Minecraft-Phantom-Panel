using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Phantom.Agent.Services.Instances;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Utils.Logging;
using Phantom.Utils.Threading;
using Serilog;

namespace Phantom.Agent.Services;

public sealed class AgentRegistrationHandler {
	private readonly ILogger logger = PhantomLogger.Create<AgentRegistrationHandler>();
	
	private readonly ManualResetEventSlim newSessionEvent = new ();
	private ImmutableArray<ConfigureInstanceMessage> lastConfigureInstanceMessages;
	
	internal void OnRegistrationComplete(ImmutableArray<ConfigureInstanceMessage> configureInstanceMessages) {
		ImmutableInterlocked.InterlockedExchange(ref lastConfigureInstanceMessages, configureInstanceMessages);
	}
	
	internal void OnNewSession() {
		newSessionEvent.Set();
	}
	
	public async Task<bool> Start(AgentServices agentServices, CancellationToken cancellationToken) {
		var configureInstanceMessages = ImmutableInterlocked.InterlockedExchange(ref lastConfigureInstanceMessages, value: default);
		if (configureInstanceMessages.IsDefault) {
			logger.Fatal("Handshake failed.");
			return false;
		}
		
		foreach (var configureInstanceMessage in configureInstanceMessages) {
			var configureInstanceResult = await agentServices.InstanceManager.Request(GetCommand(configureInstanceMessage), cancellationToken);
			if (!configureInstanceResult.Is(ConfigureInstanceResult.Success)) {
				logger.Fatal("Unable to configure instance \"{Name}\" (GUID {Guid}), shutting down.", configureInstanceMessage.Configuration.InstanceName, configureInstanceMessage.InstanceGuid);
				return false;
			}
		}
		
		agentServices.InstanceTicketManager.RefreshAgentStatus();
		
		_ = HandleNewSessionRegistrations(agentServices, cancellationToken);
		return true;
	}
	
	[SuppressMessage("ReSharper", "FunctionNeverReturns")]
	private async Task HandleNewSessionRegistrations(AgentServices agentServices, CancellationToken cancellationToken) {
		while (true) {
			await newSessionEvent.WaitHandle.WaitOneAsync(cancellationToken);
			newSessionEvent.Reset();
			
			try {
				await HandleNewSessionRegistration(agentServices, cancellationToken);
			} catch (Exception e) {
				logger.Error(e, "Could not configure instances after re-registration.");
			}
		}
	}
	
	private async Task HandleNewSessionRegistration(AgentServices agentServices, CancellationToken cancellationToken) {
		var configureInstanceMessages = ImmutableInterlocked.InterlockedExchange(ref lastConfigureInstanceMessages, value: default);
		if (configureInstanceMessages.IsDefaultOrEmpty) {
			return;
		}
		
		foreach (var configureInstanceMessage in configureInstanceMessages) {
			var configureInstanceResult = await agentServices.InstanceManager.Request(GetCommand(configureInstanceMessage), cancellationToken);
			if (!configureInstanceResult.Is(ConfigureInstanceResult.Success)) {
				logger.Error("Unable to configure instance \"{Name}\" (GUID {Guid}).", configureInstanceMessage.Configuration.InstanceName, configureInstanceMessage.InstanceGuid);
			}
		}
		
		agentServices.InstanceTicketManager.RefreshAgentStatus();
	}
	
	private static InstanceManagerActor.ConfigureInstanceCommand GetCommand(ConfigureInstanceMessage configureInstanceMessage) {
		return new InstanceManagerActor.ConfigureInstanceCommand(
			configureInstanceMessage.InstanceGuid,
			configureInstanceMessage.Configuration,
			configureInstanceMessage.LaunchProperties,
			configureInstanceMessage.LaunchNow,
			AlwaysReportStatus: true
		);
	}
}
