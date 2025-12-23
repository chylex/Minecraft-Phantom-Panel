using Akka.Actor;
using Phantom.Common.Data.Web.Agent;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Entities;
using Phantom.Controller.Database.Repositories;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc;
using Serilog;

namespace Phantom.Controller.Services.Agents;

sealed class AgentDatabaseStorageActor : ReceiveActor<AgentDatabaseStorageActor.ICommand>, IWithTimers {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentDatabaseStorageActor>();
	
	public readonly record struct Init(Guid AgentGuid, IDbContextProvider DbProvider, CancellationToken CancellationToken);
	
	public static Props<ICommand> Factory(Init init) {
		return Props<ICommand>.Create(() => new AgentDatabaseStorageActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume });
	}
	
	public ITimerScheduler Timers { get; set; } = null!;
	
	private readonly Guid agentGuid;
	private readonly IDbContextProvider dbProvider;
	private readonly CancellationToken cancellationToken;
	
	private StoreAgentRuntimeInfoCommand? storeRuntimeInfoCommand;
	
	private AgentDatabaseStorageActor(Init init) {
		this.agentGuid = init.AgentGuid;
		this.dbProvider = init.DbProvider;
		this.cancellationToken = init.CancellationToken;
		
		Receive<StoreAgentRuntimeInfoCommand>(StoreAgentRuntimeInfo);
		ReceiveAsync<StoreAgentConfigurationCommand>(StoreAgentConfiguration);
		ReceiveAsync<FlushAgentRuntimeInfoCommand>(FlushAgentRuntimeInfo);
	}
	
	private ValueTask<AgentEntity?> FindAgentEntity(ILazyDbContext db) {
		return db.Ctx.Agents.FindAsync([agentGuid], cancellationToken);
	}
	
	public interface ICommand;
	
	public sealed record StoreAgentConfigurationCommand(Guid AuditLogUserGuid, AgentConfiguration Configuration) : ICommand;
	
	public sealed record StoreAgentRuntimeInfoCommand(AgentRuntimeInfo RuntimeInfo) : ICommand;
	
	private sealed record FlushAgentRuntimeInfoCommand : ICommand;
	
	private async Task StoreAgentConfiguration(StoreAgentConfigurationCommand command) {
		await FlushAgentRuntimeInfo();
		
		bool wasCreated;
		
		await using (var db = dbProvider.Lazy()) {
			var entity = db.Ctx.AgentUpsert.Fetch(agentGuid, out wasCreated);
			
			if (wasCreated) {
				entity.AuthSecret = AuthSecret.Generate();
			}
			
			entity.Name = command.Configuration.AgentName;
			
			var auditLogWriter = new AuditLogRepository(db).Writer(command.AuditLogUserGuid);
			if (wasCreated) {
				auditLogWriter.AgentCreated(agentGuid);
			}
			else {
				auditLogWriter.AgentEdited(agentGuid);
			}
			
			await db.Ctx.SaveChangesAsync(cancellationToken);
		}
		
		string action = wasCreated ? "Created" : "Edited";
		Logger.Information(action + " agent \"{AgentName}\" (GUID {AgentGuid}) in database.", command.Configuration.AgentName, agentGuid);
	}
	
	private void StoreAgentRuntimeInfo(StoreAgentRuntimeInfoCommand command) {
		storeRuntimeInfoCommand = command;
		ScheduleFlush(TimeSpan.FromSeconds(2));
	}
	
	private void ScheduleFlush(TimeSpan delay) {
		if (storeRuntimeInfoCommand != null) {
			Timers.StartSingleTimer("FlushChanges", new FlushAgentRuntimeInfoCommand(), delay, Self);
		}
	}
	
	private Task FlushAgentRuntimeInfo(FlushAgentRuntimeInfoCommand command) {
		return FlushAgentRuntimeInfo();
	}
	
	private async Task FlushAgentRuntimeInfo() {
		if (storeRuntimeInfoCommand == null) {
			return;
		}
		
		string agentName;
		
		await using (var db = dbProvider.Lazy()) {
			var entity = await FindAgentEntity(db);
			if (entity == null) {
				return;
			}
			
			agentName = entity.Name;
			
			try {
				entity.ProtocolVersion = storeRuntimeInfoCommand.RuntimeInfo.VersionInfo?.ProtocolVersion;
				entity.BuildVersion = storeRuntimeInfoCommand.RuntimeInfo.VersionInfo?.BuildVersion;
				entity.MaxInstances = storeRuntimeInfoCommand.RuntimeInfo.MaxInstances;
				entity.MaxMemory = storeRuntimeInfoCommand.RuntimeInfo.MaxMemory;
				
				await db.Ctx.SaveChangesAsync(cancellationToken);
			} catch (Exception e) {
				ScheduleFlush(TimeSpan.FromSeconds(10));
				Logger.Error(e, "Could not update agent \"{AgentName}\" (GUID {AgentGuid}) in database.", entity.Name, agentGuid);
				return;
			}
		}
		
		Logger.Information("Updated agent \"{AgentName}\" (GUID {AgentGuid}) in database.", agentName, agentGuid);
		
		storeRuntimeInfoCommand = null;
	}
}
