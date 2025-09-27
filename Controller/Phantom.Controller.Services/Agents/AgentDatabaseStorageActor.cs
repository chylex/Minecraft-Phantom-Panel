using Phantom.Common.Data.Web.Agent;
using Phantom.Controller.Database;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Controller.Services.Agents;

sealed class AgentDatabaseStorageActor : ReceiveActor<AgentDatabaseStorageActor.ICommand> {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentDatabaseStorageActor>();
	
	public readonly record struct Init(Guid AgentGuid, IDbContextProvider DbProvider, CancellationToken CancellationToken);
	
	public static Props<ICommand> Factory(Init init) {
		return Props<ICommand>.Create(() => new AgentDatabaseStorageActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume });
	}
	
	private readonly Guid agentGuid;
	private readonly IDbContextProvider dbProvider;
	private readonly CancellationToken cancellationToken;
	
	private AgentConfiguration? configurationToStore;
	private bool hasScheduledFlush;
	
	private AgentDatabaseStorageActor(Init init) {
		this.agentGuid = init.AgentGuid;
		this.dbProvider = init.DbProvider;
		this.cancellationToken = init.CancellationToken;
		
		Receive<StoreAgentConfigurationCommand>(StoreAgentConfiguration);
		ReceiveAsync<FlushChangesCommand>(FlushChanges);
	}
	
	public interface ICommand;
	
	public sealed record StoreAgentConfigurationCommand(AgentConfiguration Configuration) : ICommand;
	
	private sealed record FlushChangesCommand : ICommand;
	
	private void StoreAgentConfiguration(StoreAgentConfigurationCommand command) {
		configurationToStore = command.Configuration;
		ScheduleFlush(TimeSpan.FromSeconds(2));
	}
	
	private async Task FlushChanges(FlushChangesCommand command) {
		hasScheduledFlush = false;
		
		if (configurationToStore == null) {
			return;
		}
		
		try {
			await using var ctx = dbProvider.Eager();
			var entity = ctx.AgentUpsert.Fetch(agentGuid);
			
			entity.Name = configurationToStore.AgentName;
			entity.ProtocolVersion = configurationToStore.ProtocolVersion;
			entity.BuildVersion = configurationToStore.BuildVersion;
			entity.MaxInstances = configurationToStore.MaxInstances;
			entity.MaxMemory = configurationToStore.MaxMemory;
			
			await ctx.SaveChangesAsync(cancellationToken);
		} catch (Exception e) {
			ScheduleFlush(TimeSpan.FromSeconds(10));
			Logger.Error(e, "Could not store agent \"{AgentName}\" (GUID {AgentGuid}) in database.", configurationToStore.AgentName, agentGuid);
			return;
		}
		
		Logger.Information("Stored agent \"{AgentName}\" (GUID {AgentGuid}) in database.", configurationToStore.AgentName, agentGuid);
		
		configurationToStore = null;
	}
	
	private void ScheduleFlush(TimeSpan delay) {
		if (!hasScheduledFlush) {
			hasScheduledFlush = true;
			Context.System.Scheduler.ScheduleTellOnce(delay, Self, new FlushChangesCommand(), Self);
		}
	}
}
